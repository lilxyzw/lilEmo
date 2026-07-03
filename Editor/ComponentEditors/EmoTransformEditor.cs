using System;
using System.Collections.Generic;
using System.Linq;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace jp.lilxyzw.lilemo
{
    [CustomEditor(typeof(EmoTransform))]
    internal class EmoTransformEditor : Editor
    {
        public VisualElement root;
        public PropertyField transforms;
        public static string TEXT_transforms => L10n.L("Transforms");

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            root.FixFont();
            root.Bind(serializedObject);
            root.Add(transforms = new PropertyField { bindingPath = "transforms", label = TEXT_transforms });
            L10n.langchanged += UpdateVisualElements;
            return root;
        }

        private void UpdateVisualElements()
        {
            if (serializedObject == null || !serializedObject.targetObject)
            {
                L10n.langchanged -= UpdateVisualElements;
                return;
            }

            transforms.label = TEXT_transforms;
            root.Bind(serializedObject);
        }

        private void OnDisable() => L10n.langchanged -= UpdateVisualElements;

        [InitializeOnLoadMethod]
        private static void AddEmoTransform()
        {
            EmoProcessor.getDefaults += GetDefaults;
            EmoProcessor.setCurves += SetCurves;
            PreviewHelper.onAnimationModePreview += OnPreview;
        }

        private static void GetDefaults(GameObject avatarRoot, Emo emo, Dictionary<(string path, string propertyName, Type type), (EditorCurveBinding binding, float value)> defaultValues)
        {
            if (!emo.GetComponent<EmoTransform>().Is(out EmoTransform emoTransform)) return;

            foreach (var t in emoTransform.transforms)
            {
                if (!t.transform) continue;
                var path = t.transform.GetPathInAvatarFast();
                if (string.IsNullOrEmpty(path)) continue;
                var rotation = TransformUtils.GetInspectorRotation(t.transform);
                if (t.position.x != t.transform.localPosition.x || t.position.y != t.transform.localPosition.y || t.position.z != t.transform.localPosition.z)
                {
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"), t.transform);
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"), t.transform);
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"), t.transform);
                }
                if (t.rotation.x != rotation.x || t.rotation.y != rotation.y || t.rotation.z != rotation.z)
                {
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"), t.transform, rotation.x);
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"), t.transform, rotation.y);
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"), t.transform, rotation.z);
                }
                if (t.scale.x != t.transform.localScale.x || t.scale.y != t.transform.localScale.y || t.scale.z != t.transform.localScale.z)
                {
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.x"), t.transform);
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.y"), t.transform);
                    EmoProcessor.AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.z"), t.transform);
                }
            }
        }

        private static void SetCurves(GameObject avatarRoot, Emo emo, KeyValuePair<(string path, string propertyName, Type type), (EditorCurveBinding binding, float value)> kv, List<AnimationCurve> curves, ref bool isAdded)
        {
            if (!isAdded && kv.Key.type == typeof(Transform) && emo.GetComponent<EmoTransform>().Is(out EmoTransform emoTransform) && emoTransform.transforms.FirstOrDefault(t => t.transform.GetPathInAvatarFast() == kv.Key.path) is EmoTransformSetting t)
            {
                if (kv.Key.propertyName == "m_LocalPosition.x") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.position.x));
                if (kv.Key.propertyName == "m_LocalPosition.y") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.position.y));
                if (kv.Key.propertyName == "m_LocalPosition.z") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.position.z));
                if (kv.Key.propertyName == "localEulerAnglesRaw.x") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.rotation.x));
                if (kv.Key.propertyName == "localEulerAnglesRaw.y") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.rotation.y));
                if (kv.Key.propertyName == "localEulerAnglesRaw.z") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.rotation.z));
                if (kv.Key.propertyName == "m_LocalScale.x") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.scale.x));
                if (kv.Key.propertyName == "m_LocalScale.y") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.scale.y));
                if (kv.Key.propertyName == "m_LocalScale.z") curves.Add(AnimationExtension.MakeZeroFrameCurve(t.scale.z));
                isAdded = true;
            }
        }

        private static void OnPreview(GameObject gameObject, GameObject avatarRoot)
        {
            if (!gameObject.GetComponent<EmoTransform>().Is(out EmoTransform emoTransform)) return;
            foreach (var t in emoTransform.transforms)
            {
                if (t.transform)
                {
                    var path = t.transform.GetPathInAvatarFast();
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.x"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.y"));
                    AnimationMode.AddEditorCurveBinding(avatarRoot, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalScale.z"));
                    t.transform.localPosition = t.position;
                    t.transform.localEulerAngles = t.rotation;
                    t.transform.localScale = t.scale;
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(EmoTransformSetting))]
    internal class EmoTransformSettingEditor : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var transform = new PropertyField { bindingPath = property.propertyPath + ".transform", label = "" };
            var position = new PropertyField { bindingPath = property.propertyPath + ".position", label = "Position" };
            var rotation = new PropertyField { bindingPath = property.propertyPath + ".rotation", label = "Rotation" };
            var scale = new PropertyField { bindingPath = property.propertyPath + ".scale", label = "Scale" };
            root.Add(transform);
            root.Add(position);
            root.Add(rotation);
            root.Add(scale);
            position.RegisterValueChangeCallback(e => PreviewHelper.StartPreview(e.changedProperty.serializedObject.targetObject));
            rotation.RegisterValueChangeCallback(e => PreviewHelper.StartPreview(e.changedProperty.serializedObject.targetObject));
            scale.RegisterValueChangeCallback(e => PreviewHelper.StartPreview(e.changedProperty.serializedObject.targetObject));

            bool isInit = true;
            transform.RegisterValueChangeCallback(e =>
            {
                if (isInit)
                {
                    isInit = false;
                    return;
                }
                if (!e.changedProperty.objectReferenceValue.Is(out Transform t)) return;
                using var position = property.FindPropertyRelative("position");
                position.vector3Value = t.localPosition;
                using var euler = property.FindPropertyRelative("rotation");
                euler.vector3Value = TransformUtils.GetInspectorRotation(t);
                using var scale = property.FindPropertyRelative("scale");
                scale.vector3Value = t.localScale;
                property.serializedObject.ApplyModifiedProperties();
                PreviewHelper.StartPreview(e.changedProperty.serializedObject.targetObject as Component);
            });
            return root;
        }
    }
}
