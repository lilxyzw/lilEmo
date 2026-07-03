using System;
using System.Collections.Generic;
using System.Linq;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace jp.lilxyzw.lilemo
{
    public static partial class EmoProcessor
    {
        public const string parameterName = "lilEmo";
        public const string parameterNameDisableBlink = "lilEmoDisableBlink";
        public const string parameterNameLeft = "GestureLeft";
        public const string parameterNameRight = "GestureRight";

        /// <summary>
        /// AnimatorControllerにパラメーターを追加する際に使用します。ExpressionsParameterには追加されません。
        /// </summary>
        public static AddParameterFunction addParameter;

        /// <summary>
        /// AnimationClipの操作内容を追加する場合、そのEditorCurveBindingとデフォルト値（つまりPrefabの値）を登録する際に使用します。
        /// </summary>
        public static GetDefaultsFunction getDefaults;

        /// <summary>
        /// 追加されたBindingに対応したCurveを登録する際に使用します。
        /// </summary>
        public static SetCurvesFunction setCurves;

        /// <summary>
        /// 独自のStateを追加する際に使用します。
        /// </summary>
        public static AddCustomStatesFunction addCustomStates;

        /// <summary>
        /// 独自のStateを追加する際に使用します。
        /// </summary>
        public static GetCustomStateConditionsFunction getCustomStateConditions;

        /// <summary>
        /// AnimatorControllerにパラメーターを追加する際に使用します。ExpressionsParameterには追加されません。
        /// </summary>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        /// <param name="emos">アバターに含まれるEmoコンポーネント全て</param>
        /// <param name="parameters">AnimatorControllerに追加するパラメーター</param>
        public delegate void AddParameterFunction(GameObject avatarRoot, Emo[] emos, HashSet<(AnimatorControllerParameterType type, string parameter)> parameters);

        /// <summary>
        /// AnimationClipの操作内容を追加する場合、そのEditorCurveBindingとデフォルト値（つまりPrefabの値）を登録する際に使用します。
        /// </summary>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        /// <param name="emo">現在処理中のEmoコンポーネント</param>
        /// <param name="defaultValues">AnimationClipのデフォルト値を登録するDictionary</param>
        public delegate void GetDefaultsFunction(GameObject avatarRoot, Emo emo, Dictionary<(string path, string propertyName, Type type), (EditorCurveBinding binding, float value)> defaultValues);

        /// <summary>
        /// 追加されたBindingに対応したCurveを登録する際に使用します。
        /// </summary>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        /// <param name="emo">現在処理中のEmoコンポーネント</param>
        /// <param name="kv">現在処理中のEditorCurveBindingとそのデフォルト値</param>
        /// <param name="curves">AnimationClipのCurve</param>
        /// <param name="isAdded">すでにCurveを登録済みかどうか。これがtrueであれば処理をスキップしてください。また、Curveを登録した場合はこの変数をtrueにしてください</param>
        public delegate void SetCurvesFunction(GameObject avatarRoot, Emo emo, KeyValuePair<(string path, string propertyName, Type type), (EditorCurveBinding binding, float value)> kv, List<AnimationCurve> curves, ref bool isAdded);

        /// <summary>
        /// 独自のStateを追加する際に使用します。
        /// </summary>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        /// <param name="emo">現在処理中のEmoコンポーネント</param>
        /// <param name="clip">現在処理中のEmoコンポーネントに対応したAnimationClip</param>
        /// <param name="info">AnimatorControllerの情報</param>
        public delegate void AddCustomStatesFunction(GameObject avatarRoot, Emo emo, AnimationClip clip, AnimatorControllerInfo info);

        /// <summary>
        /// 独自のStateを追加する際に使用します。
        /// </summary>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        /// <param name="emo">現在処理中のEmoコンポーネント</param>
        public delegate EmoCondition[][] GetCustomStateConditionsFunction(GameObject avatarRoot, Emo emo);

        /// <summary>
        /// EditorCurveBindingに基づいてデフォルト値を登録します。SerializedPropertyとEditorCurveBindingのプロパティ名が同一でない場合は登録できません。
        /// </summary>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        /// <param name="defaultValues">AnimationClipのデフォルト値を登録するDictionary</param>
        /// <param name="binding">探索に使用するEditorCurveBinding</param>
        /// <param name="obj">アニメーション対象のオブジェクト。これを渡すとAnimationUtility.GetAnimatedObject()による処理をスキップできます。</param>
        /// <param name="value">特殊なプロパティで使用。現在はlocalEulerAnglesRawの初期値を渡す。</param>
        public static void AddDefaultValueByBinding(GameObject avatarRoot, Dictionary<(string path, string propertyName, Type type), (EditorCurveBinding binding, float value)> defaultValues, EditorCurveBinding binding, Object obj = null, float value = 0)
        {
            var pair = (binding.path, binding.propertyName, binding.type);
            if (defaultValues.ContainsKey(pair) || !obj && !(obj = AnimationUtility.GetAnimatedObject(avatarRoot, binding))) return;
            if (binding.type == typeof(Transform) && obj is Transform transform && binding.propertyName.StartsWith("localEulerAnglesRaw."))
            {
                if (binding.propertyName == "localEulerAnglesRaw.x") defaultValues[pair] = (binding, transform.localEulerAngles.x);
                if (binding.propertyName == "localEulerAnglesRaw.y") defaultValues[pair] = (binding, transform.localEulerAngles.y);
                if (binding.propertyName == "localEulerAnglesRaw.z") defaultValues[pair] = (binding, transform.localEulerAngles.z);
                return;
            }
            else if (binding.type == typeof(SkinnedMeshRenderer) && binding.propertyName.StartsWith("blendShape.") && ObjHelper.TryGetRendererAndMeshWithBlendshape(obj, out var renderer, out var mesh))
            {
                var index = mesh.GetBlendShapeIndex(binding.propertyName["blendShape.".Length..]);
                if (index != -1) defaultValues[pair] = (binding, renderer.GetBlendShapeWeight(index));
            }
            else
            {
                using var so = new SerializedObject(obj);
                using var prop = so.FindProperty(binding.propertyName);
                if (prop != null)
                {
                    switch (prop.propertyType)
                    {
                        case SerializedPropertyType.Float: defaultValues[pair] = (binding, prop.floatValue); break;
                        case SerializedPropertyType.Integer: defaultValues[pair] = (binding, prop.intValue); break;
                        case SerializedPropertyType.Boolean:  defaultValues[pair] = (binding, prop.boolValue ? 1 : 0); break;
                        default: break;
                    }
                }
            }
        }

        /// <summary>
        /// カスタム条件に基づくStateを登録します。
        /// </summary>
        /// <param name="emo">現在処理中のEmoコンポーネント</param>
        /// <param name="clip">現在処理中のEmoコンポーネントに対応したAnimationClip</param>
        /// <param name="conditions">遷移に使用するcondition（AND）</param>
        /// <param name="info">AnimatorControllerの情報</param>
        public static void AddCustomConditionState(Emo emo, AnimationClip clip, EmoCondition[] conditions, AnimatorControllerInfo info)
        {
            if (conditions == null || conditions.Length == 0 || conditions.All(c => string.IsNullOrEmpty(c.parameter))) return;
            var state = info.rootStateMachine.AddState(clip.name, info.rootStateMachine.entryPosition + new Vector3(200, 100 + info.customStateIndex * 50, 0));
            state.motion = clip;
            var entry = info.rootStateMachine.AddEntryTransition(state);
            entry.conditions = conditions.Where(c => !string.IsNullOrEmpty(c.parameter)).Select(c => AnimationExtension.MakeCondition((AnimatorConditionMode)(int)c.mode, c.threshold, c.parameter)).ToArray();
            info.rootStateMachine.AddStateMachineExitTransition(info.gestureRightStateMachine).conditions = entry.conditions;
            info.rootStateMachine.AddStateMachineExitTransition(info.gestureLeftStateMachine).conditions = entry.conditions;
            info.idleState.AddTransition(info.rootStateMachine, info.dulation, entry.conditions);

            state.AddExitTransition().conditions = new[] { info.enterMenu };

            foreach (var condition in entry.conditions)
            {
                var invertedCondition = AnimationExtension.MakeInvertedCondition(condition);
                state.AddExitTransition().conditions = new[] { invertedCondition };
            }
            #if LIL_VRCSDK3A
            VRChatModule.SetTracking(state, emo.disableBlink, emo.disableLipSync, emo.disableEyeTracking, parameterNameDisableBlink);
            #endif
            info.customStateIndex++;
        }

        public class AnimatorControllerInfo
        {
            public AnimatorStateMachine rootStateMachine;
            public AnimatorStateMachine menuStateMachine;
            public AnimatorStateMachine gestureRightStateMachine;
            public AnimatorStateMachine gestureLeftStateMachine;
            public AnimatorCondition enterMenu;
            public AnimatorCondition enterRight;
            public AnimatorCondition enterLeft;
            public AnimatorCondition[] exitMenu;
            public AnimatorCondition[] exitRight;
            public AnimatorCondition[] exitLeft;
            public AnimatorState idleState;
            public float dulation;
            public int customStateIndex;
        }
    }
}
