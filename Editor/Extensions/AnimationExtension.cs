using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace jp.lilxyzw.lilemo
{
    internal static class AnimationExtension
    {
        public static AnimationCurve MakeZeroFrameCurve(float value) => new() { keys = new[]{ new Keyframe { time = 0, value = value } } };

        public static AnimatorCondition MakeCondition(AnimatorConditionMode mode, float threshold, string parameter) => new() { mode = mode, threshold = threshold, parameter = parameter };

        public static AnimatorCondition MakeInvertedCondition(AnimatorCondition condition)
        {
            return MakeCondition(
                condition.mode switch
                {
                    AnimatorConditionMode.If => AnimatorConditionMode.IfNot,
                    AnimatorConditionMode.IfNot => AnimatorConditionMode.If,
                    AnimatorConditionMode.Greater => AnimatorConditionMode.Less,
                    AnimatorConditionMode.Less => AnimatorConditionMode.Greater,
                    AnimatorConditionMode.Equals => AnimatorConditionMode.NotEqual,
                    AnimatorConditionMode.NotEqual => AnimatorConditionMode.Equals,
                    _ => AnimatorConditionMode.IfNot,
                },
                condition.threshold,
                condition.parameter
            );
        }

        public static AnimationClip MakeClip(string name, EditorCurveBinding[] bindings, AnimationCurve[] curves)
        {
            var clip = new AnimationClip { name = name };
            AnimationUtility.SetEditorCurves(clip, bindings, curves);
            return clip;
        }

        public static AnimatorState AddClipToStateMachine(this AnimatorStateMachine machine, Emo emo, AnimationClip clip, string parameterName, int value, int maxValue, float dulation, AnimatorStateMachine rootMachine, AnimatorCondition[] exitConditions)
        {
            var state = machine.AddState(clip.name, machine.entryPosition + new Vector3(200, (value - maxValue * 0.5f) * 50f, 0));
            state.motion = clip;
            machine.AddEntryTransition(state).conditions = new[] { MakeCondition(AnimatorConditionMode.Equals, value, parameterName) };
            state.AddTransition(rootMachine, dulation, MakeCondition(AnimatorConditionMode.NotEqual, value, parameterName));

            if (exitConditions != null && exitConditions.Length > 0)
                foreach (var condition in exitConditions)
                    state.AddTransition(rootMachine, dulation, condition);

            if (emo) VRChatModule.SetTracking(state, emo.disableBlink, emo.disableLipSync, emo.disableEyeTracking, EmoProcessor.parameterNameDisableBlink);
            else VRChatModule.SetTracking(state, false, false, false, EmoProcessor.parameterNameDisableBlink);

            return state;
        }

        public static void AddStateMachineExitTransitionOR(this AnimatorStateMachine machine, AnimatorStateMachine destinationStateMachine, AnimatorCondition[] conditions)
        {
            foreach (var condition in conditions)
                machine.AddStateMachineExitTransition(destinationStateMachine).conditions = new[] { condition };
        }

        public static AnimatorStateTransition AddTransition(this AnimatorState state, AnimatorState machine, float duration, params AnimatorCondition[] conditions)
        {
            var transition = state.AddTransition(machine);
            transition.hasExitTime = false;
            transition.exitTime = 0;
            transition.duration = duration;
            transition.conditions = conditions;
            return transition;
        }

        public static AnimatorStateTransition AddTransition(this AnimatorState state, AnimatorStateMachine machine, float duration, params AnimatorCondition[] conditions)
        {
            var transition = state.AddTransition(machine);
            transition.hasExitTime = false;
            transition.exitTime = 0;
            transition.duration = duration;
            transition.conditions = conditions;
            return transition;
        }
    }
}
