using System;
using System.Collections.Generic;
using System.Linq;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static jp.lilxyzw.lilemo.AnimationExtension;

namespace jp.lilxyzw.lilemo
{
    public static partial class EmoProcessor
    {
        internal static AnimatorController Process(GameObject avatarRoot, EmoSettings settings, EditorCurveBinding? blinkBinding, Action<Emo, int> addMenu)
        {
            var emos = avatarRoot.GetComponentsInChildren<Emo>(true);

            // 設定
            float dulation = 0.1f;
            if (settings)
            {
                dulation = settings.dulation;
            }
            else
            {
                dulation = 0.1f;
            }

            // コントローラー作成
            var controller = new AnimatorController { name = "lilEmo" };

            // パラメーター登録
            var parameters = new HashSet<(AnimatorControllerParameterType type, string parameter)>
            {
                (AnimatorControllerParameterType.Int, parameterNameLeft),
                (AnimatorControllerParameterType.Int, parameterNameRight),
                (AnimatorControllerParameterType.Int, parameterName),
                (AnimatorControllerParameterType.Bool, parameterNameDisableBlink)
            };
            parameters.UnionWith(emos.SelectMany(e => e.customConditions).Select(c => (c.type, c.parameter)));
            addParameter.Invoke(avatarRoot, emos, parameters);
            foreach (var parameter in parameters.Distinct(p => p.parameter))
                controller.AddParameter(parameter.parameter, parameter.type);

            // メニュー、右手、左手それぞれのStateMachineを作成
            // メニュー、カスタム、右手、左手の順で優先
            var rootStateMachine = new AnimatorStateMachine { name = "lilEmo" };
            var menuStateMachine = rootStateMachine.AddStateMachine("Menu", rootStateMachine.entryPosition + new Vector3(200, 50, 0));
            var gestureRightStateMachine = rootStateMachine.AddStateMachine("GestureRight", rootStateMachine.entryPosition + new Vector3(200, 0, 0));
            var gestureLeftStateMachine = rootStateMachine.AddStateMachine("GestureLeft", rootStateMachine.entryPosition + new Vector3(200, -50, 0));

            var enterMenu = MakeCondition(AnimatorConditionMode.NotEqual, 0, parameterName);
            var enterRight = MakeCondition(AnimatorConditionMode.NotEqual, 0, parameterNameRight);
            var enterLeft = MakeCondition(AnimatorConditionMode.NotEqual, 0, parameterNameLeft);
            var exitMenu = new[] { MakeInvertedCondition(enterMenu) };
            var exitRight = new[] { MakeInvertedCondition(enterRight), enterMenu };
            var exitLeft = new[] { MakeInvertedCondition(enterLeft), enterMenu, enterRight };

            // アニメーションされるBlendShapeと初期値を取得
            var defaultValues = new Dictionary<(string path, string propertyName, Type type), (EditorCurveBinding binding, float value)>();
            var clipBindings = new Dictionary<AnimationClip, EditorCurveBinding[]>();
            foreach (var emo in emos)
            {
                GetDefaults(avatarRoot, emo, defaultValues, clipBindings);
                getDefaults.Invoke(avatarRoot, emo, defaultValues);
            }

            // デフォルト表情
            var clipDefault = MakeClip("Neutral", defaultValues.Select(kv => kv.Value.binding).ToArray(), defaultValues.Select(kv => MakeZeroFrameCurve(kv.Value.value)).ToArray());

            // アニメーション生成、メニュー登録
            addMenu(null, 1); // Idleのメニューを最初に
            var gesturesLeft = new (AnimationClip clip, Emo emo)[8];
            var gesturesRight = new (AnimationClip clip, Emo emo)[8];
            var gestureDuplicates = new HashSet<Emo>();
            var clips = new Dictionary<Emo, AnimationClip>();
            int menuIndex = 2;
            foreach (var emo in emos)
            {
                var bindings = new List<EditorCurveBinding>();
                var curves = new List<AnimationCurve>();
                if (emo.clip)
                {
                    bindings.AddRange(clipBindings[emo.clip]);
                    curves.AddRange(clipBindings[emo.clip].Select(b => AnimationUtility.GetEditorCurve(emo.clip, b)));
                }

                foreach (var kv in defaultValues)
                {
                    if (bindings.Any(b => b.path == kv.Key.path && b.propertyName == kv.Key.propertyName)) continue;
                    bindings.Add(kv.Value.binding);

                    if (kv.Key.type == typeof(SkinnedMeshRenderer) && emo.shapes.FirstOrDefault(s => s.renderer.GetPathInAvatarFast() == kv.Key.path)?.keys.FirstOrDefault(k => $"blendShape.{k.blendshape}" == kv.Key.propertyName) is EmoKey key) curves.Add(MakeZeroFrameCurve(key.value));
                    else
                    {
                        bool isAdded = false;
                        setCurves.Invoke(avatarRoot, emo, kv, curves, ref isAdded);
                        if (!isAdded) curves.Add(MakeZeroFrameCurve(kv.Value.value));
                    }
                }

                var clip = MakeClip(emo.gameObject.name, bindings.ToArray(), curves.ToArray());
                if (emo.clip) AnimationUtility.SetAnimationClipSettings(clip, AnimationUtility.GetAnimationClipSettings(emo.clip));

                var gestureIndex = (int)emo.gesture;
                if (gestureIndex > 0)
                {
                    if (emo.hand == EmoHand.Any || emo.hand == EmoHand.Left)
                    {
                        if (gesturesLeft[gestureIndex].emo)
                        {
                            gestureDuplicates.Add(gesturesLeft[gestureIndex].emo);
                            gestureDuplicates.Add(emo);
                        }
                        gesturesLeft[gestureIndex] = (clip, emo);
                    }
                    if (emo.hand == EmoHand.Any || emo.hand == EmoHand.Right)
                    {
                        if (gesturesRight[gestureIndex].emo)
                        {
                            gestureDuplicates.Add(gesturesRight[gestureIndex].emo);
                            gestureDuplicates.Add(emo);
                        }
                        gesturesRight[gestureIndex] = (clip, emo);
                    }
                }

                menuStateMachine.AddClipToStateMachine(emo, clip, parameterName, menuIndex, emos.Length + 1, dulation, rootStateMachine, exitMenu);
                clips[emo] = clip;
                addMenu(emo, menuIndex);

                menuIndex++;
            }

            if (gestureDuplicates.Any()) Warn("There are overlapping gesture settings.", gestureDuplicates);

            // Gestureによる操作
            for (int i = 1; i < 8; i++)
            {
                {
                    var clip = gesturesLeft[i].clip;
                    var emo = gesturesLeft[i].emo;
                    if (!clip) clip = clipDefault;
                    gestureLeftStateMachine.AddClipToStateMachine(emo, clip, parameterNameLeft, i, 8, dulation, rootStateMachine, exitLeft);
                }
                {
                    var clip = gesturesRight[i].clip;
                    var emo = gesturesRight[i].emo;
                    if (!clip) clip = clipDefault;
                    gestureRightStateMachine.AddClipToStateMachine(emo, clip, parameterNameRight, i, 8, dulation, rootStateMachine, exitRight);
                }
            }

            // とりあえずIdleのStateを登録
            var idleState = rootStateMachine.AddState(clipDefault.name, rootStateMachine.entryPosition + new Vector3(200, -100, 0));

            // メニュー操作最優先
            rootStateMachine.AddEntryTransition(menuStateMachine).conditions = new[] { enterMenu };

            // 次にカスタム
            var info = new AnimatorControllerInfo{

                rootStateMachine = rootStateMachine,
                menuStateMachine = menuStateMachine,
                gestureRightStateMachine = gestureRightStateMachine,
                gestureLeftStateMachine = gestureLeftStateMachine,
                enterMenu = enterMenu,
                enterRight = enterRight,
                enterLeft = enterLeft,
                exitMenu = exitMenu,
                exitRight = exitRight,
                exitLeft = exitLeft,
                idleState = idleState,
                dulation = dulation,
                customStateIndex = 0
            };

            foreach (var emo in emos)
            {
                // カスタムパラメーターによる遷移
                var clip = clips[emo];
                AddCustomConditionState(emo, clip, emo.customConditions, info);
                addCustomStates(avatarRoot, emo, clip, info);
            }

            // 次にGesture
            rootStateMachine.AddEntryTransition(gestureRightStateMachine).conditions = new[] { enterRight };
            rootStateMachine.AddEntryTransition(gestureLeftStateMachine).conditions = new[] { enterLeft };

            // 最後にIdle
            idleState.motion = clipDefault;
            rootStateMachine.defaultState = idleState;
            rootStateMachine.AddEntryTransition(idleState);
            idleState.AddExitTransition().conditions = new[] { enterMenu };
            idleState.AddExitTransition().conditions = new[] { enterRight };
            idleState.AddExitTransition().conditions = new[] { enterLeft };
            VRChatModule.SetTracking(idleState, false, false, false, parameterNameDisableBlink);
            menuStateMachine.AddClipToStateMachine(null, clipDefault, parameterName, 1, emos.Length + 1, dulation, rootStateMachine, exitMenu);

            rootStateMachine.AddStateMachineExitTransitionOR(menuStateMachine, exitMenu);
            rootStateMachine.AddStateMachineExitTransitionOR(gestureRightStateMachine, exitRight);
            rootStateMachine.AddStateMachineExitTransitionOR(gestureLeftStateMachine, exitLeft);

            // AnimatorControllerに追加
            controller.AddLayer(new AnimatorControllerLayer
            {
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 1,
                name = "lilEmo",
                stateMachine = rootStateMachine
            });

            // まばたきレイヤー
            AnimationClip enableBlinkClip = null;
            AnimationClip disableBlinkClip = null;
            if (blinkBinding is EditorCurveBinding blinkBinding2 && AnimationUtility.GetAnimatedObject(avatarRoot, blinkBinding2) is SkinnedMeshRenderer face && face.sharedMesh is Mesh faceMesh)
            {
                var index = faceMesh.GetBlendShapeIndex(blinkBinding2.propertyName["blendShape.".Length..]);
                var weight = faceMesh.GetBlendShapeFrameWeight(index, faceMesh.GetBlendShapeFrameCount(index) - 1);
                var disableCurve = MakeZeroFrameCurve(0);
                var enableCurve = new AnimationCurve();
                enableCurve.AddKey(new(0f, 0, 1f, 1f));
                enableCurve.AddKey(new(1f, 0, 1f, 1f));
                enableCurve.AddKey(new(1.05f, weight, 1f, 1f));
                enableCurve.AddKey(new(1.1f, weight, 1f, 1f));
                enableCurve.AddKey(new(1.2f, 0, 1f, 1f));
                enableCurve.AddKey(new(5f, 0, 1f, 1f));

                enableBlinkClip = MakeClip("Enable", new[] { blinkBinding2 }, new[] { enableCurve });
                disableBlinkClip = MakeClip("Disable", new[] { blinkBinding2 }, new[] { disableCurve });
                var setting = AnimationUtility.GetAnimationClipSettings(enableBlinkClip);
                setting.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(enableBlinkClip, setting);
            }
            var blinkStateMachine = new AnimatorStateMachine { name = "lilEmoBlink" };
            var enableState = blinkStateMachine.AddState("Enable", blinkStateMachine.entryPosition + new Vector3(200, 0, 0));
            enableState.motion = enableBlinkClip;
            var disableState = blinkStateMachine.AddState("Disable", blinkStateMachine.entryPosition + new Vector3(500, 0, 0));
            disableState.motion = disableBlinkClip;

            blinkStateMachine.defaultState = enableState;
            enableState.AddTransition(disableState, 0f, MakeCondition(AnimatorConditionMode.If, 0, parameterNameDisableBlink));
            disableState.AddTransition(enableState, 0f, MakeCondition(AnimatorConditionMode.IfNot, 0, parameterNameDisableBlink));
            controller.AddLayer(new AnimatorControllerLayer
            {
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 1,
                name = "lilEmoBlink",
                stateMachine = blinkStateMachine
            });

            return controller;
        }

        private static void GetDefaults(GameObject avatarRoot, Emo emo, Dictionary<(string path, string propertyName, Type type), (EditorCurveBinding binding, float value)> defaultValues, Dictionary<AnimationClip, EditorCurveBinding[]> clipBindings)
        {
            if (emo.clip) foreach (var binding in clipBindings[emo.clip] = AnimationUtility.GetCurveBindings(emo.clip)) AddDefaultValueByBinding(avatarRoot, defaultValues, binding);

            foreach (var shape in emo.shapes)
            {
                if (!ObjHelper.TryGetRendererAndMeshWithBlendshape(shape.renderer, out var renderer, out var mesh)) continue;
                foreach (var key in shape.keys) AddDefaultValueByBinding(avatarRoot, defaultValues, EditorCurveBinding.FloatCurve(renderer.GetPathInAvatarFast(), typeof(SkinnedMeshRenderer), $"blendShape.{key.blendshape}"), shape.renderer);
            }
        }

        private static void Error(string key, params object[] args) => Report(nadena.dev.ndmf.ErrorSeverity.Error, key, args);
        private static void Warn(string key, params object[] args) => Report(nadena.dev.ndmf.ErrorSeverity.NonFatal, key, args);
        private static void Report(nadena.dev.ndmf.ErrorSeverity severity, string key, params object[] args)
        {
            var list = L10n.GetLanguages().Select(code => (code, (Func<string, string>)(key => L10n.L(key, code)))).ToList();
            var localizer = new nadena.dev.ndmf.localization.Localizer("en-US", () => list);
            nadena.dev.ndmf.ErrorReport.ReportError(localizer, severity, key, args);
        }
    }
}
