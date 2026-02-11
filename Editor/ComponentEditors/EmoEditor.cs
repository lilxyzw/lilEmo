using System.Collections.Generic;
using System.Linq;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace jp.lilxyzw.lilemo
{
    [CustomEditor(typeof(Emo))]
    internal class EmoEditor : Editor
    {
        public VisualElement root;
        public PropertyField gesture;
        public PropertyField hand;
        public PropertyField disableBlink;
        public PropertyField disableLipSync;
        public PropertyField disableEyeTracking;
        public PropertyField customConditions;
        public PropertyField clip;
        public ListView shapes;

        public static string TEXT_gesture => L10n.L("Gesture");
        public static string TEXT_hand => L10n.L("Hand");
        public static string TEXT_disableBlink => L10n.L("Disable Blink");
        public static string TEXT_disableLipSync => L10n.L("Disable LipSync");
        public static string TEXT_disableEyeTracking => L10n.L("Disable Eye Tracking");
        public static string TEXT_customConditions => L10n.L("Custom Conditions");
        public static string TEXT_clip => L10n.L("Animation Clip");
        public static string TEXT_transforms => L10n.L("Transforms");

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            root.FixFont();
            root.Bind(serializedObject);
            root.Add(L10n.SelectionGUI());
            root.Add(gesture = new PropertyField { bindingPath = "gesture", label = TEXT_gesture });
            root.Add(hand = new PropertyField { bindingPath = "hand", label = TEXT_hand });
            root.Add(disableBlink = new PropertyField { bindingPath = "disableBlink", label = TEXT_disableBlink });
            root.Add(disableLipSync = new PropertyField { bindingPath = "disableLipSync", label = TEXT_disableLipSync });
            root.Add(disableEyeTracking = new PropertyField { bindingPath = "disableEyeTracking", label = TEXT_disableEyeTracking });
            root.Add(customConditions = new PropertyField { bindingPath = "customConditions", label = TEXT_customConditions });
            root.Add(clip = new PropertyField { bindingPath = "clip", label = TEXT_clip });
            root.Add(shapes = new ListView
            {
                bindingPath = "shapes",
                selectionType = SelectionType.Multiple,
                showAddRemoveFooter = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            });

            // 無視される設定はオフに
            gesture.RegisterValueChangeCallback((e) => hand.SetEnabled(e.changedProperty.intValue != (int)EmoGesture.MenuOnly));

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

            gesture.label = TEXT_gesture;
            hand.label = TEXT_hand;
            disableBlink.label = TEXT_disableBlink;
            disableLipSync.label = TEXT_disableLipSync;
            disableEyeTracking.label = TEXT_disableEyeTracking;
            customConditions.label = TEXT_customConditions;
            clip.label = TEXT_clip;
            root.Bind(serializedObject);
        }

        private void OnDisable() => L10n.langchanged -= UpdateVisualElements;
    }

    [CustomPropertyDrawer(typeof(EmoShape))]
    internal class EmoShapeEditor : PropertyDrawer
    {
        private SerializedProperty property;
        public VisualElement root;
        public PropertyField renderer;
        public ToolbarSearchField search;
        public ScrollView shapes;

        public static string TEXT_renderer => L10n.L("Renderer");

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.property = property;
            root = new VisualElement();
            root.Add(renderer = new PropertyField { bindingPath = property.propertyPath + ".renderer", label = TEXT_renderer });
            root.Add(search = new ToolbarSearchField());
            root.Add(shapes = new ScrollView { horizontalScrollerVisibility = ScrollerVisibility.Hidden });
            shapes.style.maxHeight = 600;

            renderer.RegisterValueChangeCallback((_) => SetShapes());
            search.style.width = new StyleLength(StyleKeyword.Auto);
            search.RegisterValueChangedCallback((e) =>
            {
                foreach (Slider slider in shapes.Children())
                    slider.style.display = string.IsNullOrEmpty(e.newValue) || slider.label.Contains(e.newValue, System.StringComparison.OrdinalIgnoreCase) ? DisplayStyle.Flex : DisplayStyle.None;
            });
            Undo.undoRedoPerformed += UndoRedoPerformed;
            return root;
        }

        private void UpdateVisualElements()
        {
            if (renderer == null)
            {
                L10n.langchanged -= UpdateVisualElements;
                return;
            }

            renderer.label = TEXT_renderer;
        }

        private void SetShapes()
        {
            // 登録済みのBlendShapeを取得
            property.serializedObject.Update();
            using var keys = property.FindPropertyRelative("keys");
            var arraySize = keys.arraySize;
            var shapeList = new (string, SerializedProperty)[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                using var blendshape = keys.FindPropertyRelative($"Array.data[{i}].blendshape");
                var value = keys.FindPropertyRelative($"Array.data[{i}].value");
                shapeList[i] = (blendshape.stringValue, value);
            }

            shapes.Clear();
            // メッシュのBlendShapeを取得
            using var r = property.FindPropertyRelative("renderer");
            if (r.objectReferenceValue is SkinnedMeshRenderer renderer && renderer.sharedMesh is Mesh mesh)
            {
                int shapeCount = mesh.blendShapeCount;
                for (int i = 0; i < shapeCount; i++)
                {
                    var name = mesh.GetBlendShapeName(i);
                    var slider = new Slider
                    {
                        label = name,
                        lowValue = Mathf.Min(0, mesh.GetBlendShapeFrameWeight(i, 0)),
                        highValue = mesh.GetBlendShapeFrameWeight(i, mesh.GetBlendShapeFrameCount(i) - 1),
                        showInputField = true
                    };
                    shapes.Add(slider);

                    // 削除ボタン
                    var button = new Button(() => Button(slider)) { text = "x" };
                    slider.Add(button);

                    var value = shapeList.FirstOrDefault(kv => kv.Item1 == name).Item2;
                    if (value != null)
                    {
                        slider.BindProperty(value);
                        slider.style.unityFontStyleAndWeight = FontStyle.Bold;
                        button.SetEnabled(true);
                    }
                    else
                    {
                        slider.value = renderer.GetBlendShapeWeight(i);
                        button.SetEnabled(false);
                        bool isInit = true;
                        void ValueChangedCallback(ChangeEvent<float> e)
                        {
                            if (isInit)
                            {
                                isInit = false;
                                return;
                            }
                            using var keys = property.FindPropertyRelative("keys");
                            keys.arraySize++;
                            using var blendshape = keys.FindPropertyRelative($"Array.data[{keys.arraySize - 1}].blendshape");
                            using var value = keys.FindPropertyRelative($"Array.data[{keys.arraySize - 1}].value");
                            blendshape.stringValue = slider.label;
                            value.floatValue = e.newValue;
                            property.serializedObject.ApplyModifiedProperties();
                            slider.BindProperty(value);
                            slider.style.unityFontStyleAndWeight = FontStyle.Bold;
                            button.SetEnabled(true);
                            slider.UnregisterValueChangedCallback(ValueChangedCallback);
                        }
                        slider.RegisterValueChangedCallback(ValueChangedCallback);
                    }
                }
            }
        }

        private void Button(Slider slider)
        {
            using var keys = property.FindPropertyRelative("keys");
            var arraySize = keys.arraySize;
            for (int i = 0; i < arraySize; i++)
            {
                using var blendshape = keys.FindPropertyRelative($"Array.data[{i}].blendshape");
                if (blendshape.stringValue == slider.label)
                {
                    keys.DeleteArrayElementAtIndex(i);
                    property.serializedObject.ApplyModifiedProperties();
                    break;
                }
            }
            SetShapes();
        }

        void UndoRedoPerformed()
        {
            try { SetShapes(); }
            catch { Undo.undoRedoPerformed -= UndoRedoPerformed; }
        }
    }

    [CustomPropertyDrawer(typeof(EmoCondition))]
    internal class EmoConditionEditor : PropertyDrawer
    {
        private static List<int> m_boolModes = new() { (int)EmoConditionMode.If, (int)EmoConditionMode.IfNot };
        private static List<int> m_floatModes = new() { (int)EmoConditionMode.Greater, (int)EmoConditionMode.Less };
        private static List<int> m_intModes = new() { (int)EmoConditionMode.Greater, (int)EmoConditionMode.Less, (int)EmoConditionMode.Equals, (int)EmoConditionMode.NotEqual };

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var type = new PropertyField { bindingPath = property.propertyPath + ".type", label = "" };
            type.style.width = 66;
            root.Add(type);

            var parameter = new PropertyField { bindingPath = property.propertyPath + ".parameter", label = "" };
            parameter.style.flexGrow = 1;
            root.Add(parameter);

            var mode = new PopupField<int> { bindingPath = property.propertyPath + ".mode", label = "" };
            mode.style.width = 80;
            mode.formatListItemCallback = (i) => ((EmoConditionMode)i).ToString();
            mode.formatSelectedValueCallback = (i) => ((EmoConditionMode)i).ToString();
            root.Add(mode);

            var threshold = new PropertyField { bindingPath = property.propertyPath + ".threshold", label = "" };
            threshold.style.flexGrow = 0.5f;

            bool isInit = true;
            type.RegisterValueChangeCallback((e) =>
            {
                switch (e.changedProperty.intValue)
                {
                    case (int)AnimatorControllerParameterType.Float:
                        threshold.visible = true;
                        mode.visible = true;
                        mode.choices = m_floatModes;
                        if (!isInit) mode.value = (int)EmoConditionMode.Greater;
                        break;
                    case (int)AnimatorControllerParameterType.Int:
                        threshold.visible = true;
                        mode.visible = true;
                        mode.choices = m_intModes;
                        if (!isInit) mode.value = (int)EmoConditionMode.Equals;
                        break;
                    case (int)AnimatorControllerParameterType.Bool:
                        threshold.visible = false;
                        mode.visible = true;
                        mode.choices = m_boolModes;
                        if (!isInit) mode.value = (int)EmoConditionMode.If;
                        break;
                    case (int)AnimatorControllerParameterType.Trigger:
                        threshold.visible = false;
                        mode.visible = false;
                        if (!isInit) mode.value = (int)EmoConditionMode.If;
                        break;
                }

                if (isInit)
                {
                    isInit = false;
                    return;
                }
            });

            root.Add(threshold);
            return root;
        }
    }
}
