#if LIL_VRCSDK3A
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;

namespace jp.lilxyzw.lilemo
{
    internal static class VRChatModule
    {
        public static VRCAvatarDescriptor GetDescriptor(GameObject root) => root.GetComponent<VRCAvatarDescriptor>();
        public static float GetViewHeight(GameObject root) => GetDescriptor(root).ViewPosition.y;

        public static float GetFaceSize(GameObject root)
        {
            if (GetDescriptor(root).Is(out VRCAvatarDescriptor descriptor) &&
                descriptor.customEyeLookSettings.eyelidsSkinnedMesh.Is(out SkinnedMeshRenderer renderer) &&
                renderer.sharedMesh.Is(out Mesh mesh)
            ) return mesh.bounds.extents.y;
            return 0.1f;
        }

        public static void Setup(GameObject root, out EditorCurveBinding? blinkBinding)
        {
            var descriptor = GetDescriptor(root);
            if (descriptor.customEyeLookSettings.eyelidsBlendshapes == null ||
                descriptor.customEyeLookSettings.eyelidsBlendshapes.Length == 0 ||
                descriptor.customEyeLookSettings.eyelidsBlendshapes[0] == -1 ||
                !ObjHelper.TryGetRendererAndMeshWithBlendshape(descriptor.customEyeLookSettings.eyelidsSkinnedMesh, out var renderer, out var mesh) ||
                descriptor.customEyeLookSettings.eyelidsBlendshapes[0] > mesh.blendShapeCount)
            {
                blinkBinding = null;
                return;
            }

            var blendshape = mesh.GetBlendShapeName(descriptor.customEyeLookSettings.eyelidsBlendshapes[0]);
            blinkBinding = new() { path = renderer.GetPathInAvatarFast(), propertyName = $"blendShape.{blendshape}", type = typeof(SkinnedMeshRenderer) };
            descriptor.customEyeLookSettings.eyelidsBlendshapes[0] = -1;
        }

        public static void SetTracking(AnimatorState state, bool disableBlink, bool disableLipSync, bool disableEyeTracking, string parameterNameDisableBlink)
        {
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters.Add(new VRC_AvatarParameterDriver.Parameter
            {
                type = VRC_AvatarParameterDriver.ChangeType.Set,
                name = parameterNameDisableBlink,
                value = disableBlink ? 1 : 0
            });

            var control = state.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
            control.trackingMouth = disableLipSync ? VRC_AnimatorTrackingControl.TrackingType.Animation : VRC_AnimatorTrackingControl.TrackingType.Tracking;
            control.trackingEyes = disableEyeTracking ? VRC_AnimatorTrackingControl.TrackingType.Animation : VRC_AnimatorTrackingControl.TrackingType.Tracking;
        }
    }
}
#endif
