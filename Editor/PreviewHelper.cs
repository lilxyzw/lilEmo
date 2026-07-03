using System.Collections.Generic;
using System.Linq;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEngine;

namespace jp.lilxyzw.lilemo
{
    public partial class PreviewHelper : ScriptableSingleton<PreviewHelper>
    {
        [SerializeField] private AnimationModeDriver driver;
        private static AnimationModeDriver Driver => instance.driver ? instance.driver : instance.driver = CreateInstance<AnimationModeDriver>();

        private static void StartPreview(GameObject gameObject)
        {
            AnimationMode.StopAnimationMode(Driver);
            if (!gameObject || !gameObject.GetAvatarRoot().Is(out Transform rootTransform) || !rootTransform.gameObject.Is(out GameObject root)) return;
            AnimationMode.StartAnimationMode(Driver);
            onAnimationModePreview?.Invoke(gameObject, root);
        }

        // NDMF Preview
        internal static void PreviewEmo(Emo emo, SkinnedMeshRenderer original, SkinnedMeshRenderer renderer, Mesh mesh)
        {
            if (emo.clip)
            {
                var path = original.GetPathInAvatarFast();
                var bindings = AnimationUtility.GetCurveBindings(emo.clip);
                foreach (var binding in bindings)
                {
                    if (binding.path != path || !binding.propertyName.StartsWith("blendShape.")) continue;

                    var index = mesh.GetBlendShapeIndex(binding.propertyName["blendShape.".Length..]);
                    if (index == -1) continue;
                    renderer.SetBlendShapeWeight(index, AnimationUtility.GetEditorCurve(emo.clip, binding).keys.First().value);
                }
            }

            if (emo.shapes.FirstOrDefault(s => s.renderer == original) is EmoShape shape)
                foreach (var key in shape.keys)
                {
                    var index = mesh.GetBlendShapeIndex(key.blendshape);
                    if (index == -1) continue;
                    renderer.SetBlendShapeWeight(index, key.value);
                }

            onNDMFPreview?.Invoke(emo, original, renderer);
        }

        // Menu Icon
        private static HashSet<(SkinnedMeshRenderer renderer, int index, float value)> PreviewEmo(Emo emo, GameObject root)
        {
            var modifiedShapes = new HashSet<(SkinnedMeshRenderer renderer, int index, float value)>();
            if (emo.clip)
            {
                var bindings = AnimationUtility.GetCurveBindings(emo.clip);
                foreach (var binding in bindings)
                {
                    if (!binding.propertyName.StartsWith("blendShape.") || !ObjHelper.TryGetRendererAndMeshWithBlendshape(AnimationUtility.GetAnimatedObject(root, binding), out var renderer, out var mesh)) continue;

                    var index = mesh.GetBlendShapeIndex(binding.propertyName["blendShape.".Length..]);
                    if (index == -1) continue;
                    modifiedShapes.Add((renderer, index, renderer.GetBlendShapeWeight(index)));
                    renderer.SetBlendShapeWeight(index, AnimationUtility.GetEditorCurve(emo.clip, binding).keys.First().value);
                }
            }

            foreach (var shape in emo.shapes)
            {
                if (!ObjHelper.TryGetRendererAndMeshWithBlendshape(shape.renderer, out var renderer, out var mesh)) continue;
                foreach (var key in shape.keys)
                {
                    var index = mesh.GetBlendShapeIndex(key.blendshape);
                    if (index == -1) continue;
                    modifiedShapes.Add((renderer, index, renderer.GetBlendShapeWeight(index)));
                    renderer.SetBlendShapeWeight(index, key.value);
                }
            }

            onCapture?.Invoke(emo, root, modifiedShapes);
            return modifiedShapes;
        }

       internal static Dictionary<Emo, Texture2D> Capture(GameObject root, EmoSettings settings)
        {
            var captureSize = 128;
            if (settings) captureSize = settings.iconSize;
            var currentRT = RenderTexture.active;

            #if LIL_VRCSDK3A
            var viewHeight = VRChatModule.GetViewHeight(root);
            var faceSize = VRChatModule.GetFaceSize(root);
            #elif LIL_BASISSDK
            var viewHeight = BasisModule.GetViewHeight(root);
            var faceSize = BasisModule.GetFaceSize(root);
            #endif

            var cameraObj = new GameObject();
            var camera = cameraObj.AddComponent<Camera>();
            if (settings && settings.iconCaptureCamera)
            {
                cameraObj.transform.SetPositionAndRotation(settings.iconCaptureCamera.gameObject.transform.position, settings.iconCaptureCamera.gameObject.transform.rotation);
                camera.CopyFrom(settings.iconCaptureCamera);
                if (settings.iconCaptureCamera.GetComponent<AudioListener>() is AudioListener l) DestroyImmediate(l);
                DestroyImmediate(settings.iconCaptureCamera);
            }
            else
            {
                cameraObj.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
                cameraObj.transform.position += cameraObj.transform.up * viewHeight + cameraObj.transform.forward * 2;
                cameraObj.transform.Rotate(Vector3.up, 180, Space.Self);

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.orthographic = true;
                camera.orthographicSize = faceSize;
                camera.fieldOfView = 10;
                camera.nearClipPlane = 0.001f;
                camera.allowHDR = true;
                camera.allowMSAA = true;
            }

            var renderTexture = RenderTexture.GetTemporary(captureSize, captureSize, 24);
            renderTexture.antiAliasing = 8;
            RenderTexture.active = renderTexture;
            camera.targetTexture = renderTexture;
            camera.cullingMask = 1 << 31;

            var light = cameraObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1;
            light.renderMode = LightRenderMode.ForceVertex;

            var icons = new Dictionary<Emo, Texture2D>();
            foreach (var emo in root.GetComponentsInChildren<Emo>())
            {
                var modifiedShapes = PreviewEmo(emo, root);

                // 初回にレンダリングした表情のまま固定される
                // おそらくSkinnedMeshRendererの更新が初回しかされない
                // 非効率だが毎回クローンを生成してからレンダリング
                var clone = Instantiate(root);
                foreach (var t in clone.GetComponentsInChildren<Renderer>()) t.gameObject.layer = 31;
                camera.Render();
                DestroyImmediate(clone);

                foreach (var (renderer, index, value) in modifiedShapes) renderer.SetBlendShapeWeight(index, value);
                var icon = new Texture2D(captureSize, captureSize, TextureFormat.RGBA32, false, false) { name = emo.gameObject.name };
                icon.ReadPixels(new Rect(0, 0, captureSize, captureSize), 0, 0);
                icon.Apply();
                icon.Compress(false);

                icons[emo] = icon;
            }

            RenderTexture.active = currentRT;
            DestroyImmediate(cameraObj);

            return icons;
        }
    }
}
