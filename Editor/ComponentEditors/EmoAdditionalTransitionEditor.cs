using System.Linq;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace jp.lilxyzw.lilemo
{
    [CustomEditor(typeof(EmoAdditionalTransition))]
    internal class EmoAdditionalTransitionEditor : Editor
    {
        public VisualElement root;
        public PropertyField customConditions;
        public static string TEXT_customConditions => L10n.L("Custom Conditions");

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            root.FixFont();
            root.Bind(serializedObject);
            root.Add(customConditions = new PropertyField { bindingPath = "customConditions", label = TEXT_customConditions });
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

            customConditions.label = TEXT_customConditions;
            root.Bind(serializedObject);
        }

        private void OnDisable() => L10n.langchanged -= UpdateVisualElements;

        [InitializeOnLoadMethod]
        private static void AddEmoTransform()
        {
            // 処理用
            EmoProcessor.addParameter += (avatarRoot, emos, parameters) => parameters.UnionWith(emos.SelectMany(e => e.GetComponents<EmoAdditionalTransition>()).SelectMany(c => c.customConditions).Select(c => (c.type, c.parameter)));
            EmoProcessor.addCustomStates += (avatarRoot, emo, clip, info) =>
            {
                foreach (var t in emo.GetComponents<EmoAdditionalTransition>()) EmoProcessor.AddCustomConditionState(emo, clip, t.customConditions, info);
            };
        }
    }
}
