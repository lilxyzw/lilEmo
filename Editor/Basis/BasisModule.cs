#if LIL_BASISSDK
using System;
using System.Collections.Generic;
using System.Linq;
using Basis.Scripts.BasisSdk;
using jp.lilxyzw.emock;
using jp.lilxyzw.emock.Editor;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEngine;
using static jp.lilxyzw.lilemo.AnimationExtension;

namespace jp.lilxyzw.lilemo
{
    internal static class BasisModule
    {
        public static BasisAvatar GetDescriptor(GameObject root) => root.GetComponent<BasisAvatar>();
        public static float GetViewHeight(GameObject root) => GetDescriptor(root).AvatarEyePosition.x;

        public static float GetFaceSize(GameObject root)
        {
            if (GetDescriptor(root).Is(out BasisAvatar descriptor) &&
                descriptor.FaceVisemeMesh.Is(out SkinnedMeshRenderer renderer) &&
                renderer.sharedMesh.Is(out Mesh mesh)
            ) return mesh.bounds.extents.y;
            return 0.1f;
        }
    }

    #if LIL_EMOCK
    public static partial class EmoProcessor
    {
        public static void ProcessEmock(GameObject avatarRoot)
        {
            if (!avatarRoot.GetComponentInChildren<Emo>(true)) return;

            var emockAnimator = avatarRoot.GetComponentInChildren<EmockAnimator>();
            if (!emockAnimator)
            {
                emockAnimator = new GameObject("EmockAnimator").AddComponent<EmockAnimator>();
                emockAnimator.transform.parent = avatarRoot.transform;
            }

            var emockNetwork = avatarRoot.GetComponentInChildren<EmockNetwork>();
            if (!emockNetwork)
            {
                emockNetwork = new GameObject("EmockNetwork").AddComponent<EmockNetwork>();
                emockNetwork.transform.parent = avatarRoot.transform;
                emockNetwork.animator = emockAnimator;
            }

            var emockController = avatarRoot.GetComponentInChildren<EmockController>();
            if (!emockController)
            {
                emockController = new GameObject("EmockController").AddComponent<EmockController>();
                emockController.transform.parent = avatarRoot.transform;
                emockController.emockNetwork = emockNetwork;
            }

            float dulation = 0.1f;
            var settings = avatarRoot.GetComponentInChildren<EmoSettings>(true);
            if (settings) dulation = settings.dulation;

            var emos = avatarRoot.GetComponentsInChildren<Emo>(true);

            // アニメーション変換
            ConvertToClips(emos, avatarRoot, null, null, out var clipDefault, out var clips, out var gesturesLeft, out var gesturesRight);

            // 変換したアニメーションからさらにEmockコンポーネントに変換
            var rightHands = new List<EmockState>();
            var leftHands = new List<EmockState>();

            var animatedBlendShape = new Dictionary<SkinnedMeshRenderer, HashSet<string>>();
            var animatedTransform = new HashSet<Transform>();

            var emockClips = new List<EmockClip>{new()};

            var icons = PreviewHelper.Capture(avatarRoot, settings);
            var menuItemObject = new GameObject();
            menuItemObject.transform.parent = avatarRoot.transform;
            {
                var menuItem = menuItemObject.AddComponent<EmockMenuItem>();

                menuItem.title = "Unlock";
                menuItem.description = "Unlock";
                menuItem.parameter = "lilEmo";
                menuItem.controller = emockController;
                menuItem.saveType = emock.SaveType.None;
                menuItem.defaultValue = 0;
            }

            var fixStates = new List<EmockState>();
            var fixGroup = new EmockStateGroup{conditions = new EmockConditions[]{new(){name = "lilEmo", value = 0, mode = EmockConditionMode.NotEqual}}};

            var groups = new List<EmockStateGroup>{fixGroup};
            ushort index = 1;
            foreach (var emo in emos)
            {
                EmockClip clip = EmockClipConverter.FromAnimationClip(clips[emo], avatarRoot);

                clip.fadein = dulation;
                clip.disableBlink = emo.disableBlink;
                clip.disableLipSync = emo.disableLipSync;
                clip.disableEyeTracking = emo.disableEyeTracking;
                emockClips.Add(clip);

                fixStates.Add(new(){index = index, conditions = new EmockConditions[]{new(){name = "lilEmo", value = index, mode = EmockConditionMode.Equals}}});

                if (emo.customConditions == null || emo.customConditions.Length == 0 || emo.customConditions.All(c => string.IsNullOrEmpty(c.parameter)))
                {
                    // Nothing to do
                }
                else
                {
                    groups.Add(new()
                    {
                        states = new EmockState[]{new(){index = index}},
                        conditions = emo.customConditions.Select(c => new EmockConditions{name = c.parameter, value = c.threshold, mode = (EmockConditionMode)c.mode}).ToArray()
                    });
                }

                foreach (var conditions in getCustomStateConditions(avatarRoot, emo))
                {
                    groups.Add(new()
                    {
                        states = new EmockState[]{new(){index = index}},
                        conditions = conditions.Select(c => new EmockConditions{name = c.parameter, value = c.threshold, mode = (EmockConditionMode)c.mode}).ToArray()
                    });
                }

                var gestureIndex = (int)emo.gesture;
                if (gestureIndex > 0)
                {
                    if (emo.hand == EmoHand.Any || emo.hand == EmoHand.Left)
                        leftHands.Add(new(){conditions = new EmockConditions[]{new(){name = "LeftHand", value = (float)emo.gesture, mode = EmockConditionMode.Equals}}, index = index});

                    if (emo.hand == EmoHand.Any || emo.hand == EmoHand.Right)
                        rightHands.Add(new(){conditions = new EmockConditions[]{new(){name = "RightHand", value = (float)emo.gesture, mode = EmockConditionMode.Equals}}, index = index});
                }

                var menuItem = menuItemObject.AddComponent<EmockMenuItem>();

                menuItem.title = emo.name;
                menuItem.description = emo.name;
                menuItem.parameter = "lilEmo";
                menuItem.controller = emockController;
                menuItem.saveType = emock.SaveType.None;
                menuItem.defaultValue = index;

                var icon = icons[emo];
                menuItem.icon = Sprite.Create(icon, new(0,0,icon.width,icon.height), Vector2.zero);

                index++;
            }

            fixGroup.states = fixStates.ToArray();

            groups.Add(new(){states = rightHands.ToArray(), conditions = new EmockConditions[]{new(){name = "RightHand", value = 0, mode = EmockConditionMode.NotEqual}}});
            groups.Add(new(){states = leftHands.ToArray()});

            emockController.groups = groups.ToArray();

            emockAnimator.clips = emockClips.ToArray();
        }
    }
    #endif
}
#endif
