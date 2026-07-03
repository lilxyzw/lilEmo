using System;
using UnityEngine;

namespace jp.lilxyzw.lilemo.runtime
{
    [ExecuteInEditMode][DisallowMultipleComponent]
    public class Emo : EmoEditorOnly
    {
        public EmoGesture gesture = EmoGesture.MenuOnly;
        public EmoHand hand = EmoHand.Any;
        public bool disableBlink = false;
        public bool disableLipSync = false;
        public bool disableEyeTracking = false;
        public EmoCondition[] customConditions = new EmoCondition[] { };
        public AnimationClip clip;
        public EmoShape[] shapes;

#if UNITY_EDITOR && LIL_VRCSDK3
        private void Start()
        {
            if (shapes == null && UnityEditor.Selection.activeGameObject && UnityEditor.Selection.activeGameObject.GetComponentInParent<VRC.SDKBase.VRC_AvatarDescriptor>() is VRC.SDKBase.VRC_AvatarDescriptor descriptor && descriptor && descriptor.VisemeSkinnedMesh is SkinnedMeshRenderer renderer && renderer)
                shapes = new EmoShape[] { new() { renderer = renderer } };
        }
#elif UNITY_EDITOR && LIL_BASISSDK
        private void Start()
        {
            if (shapes == null && UnityEditor.Selection.activeGameObject && UnityEditor.Selection.activeGameObject.GetComponentInParent<Basis.Scripts.BasisSdk.BasisAvatar>() is Basis.Scripts.BasisSdk.BasisAvatar descriptor && descriptor && descriptor.FaceVisemeMesh is SkinnedMeshRenderer renderer && renderer)
                shapes = new EmoShape[] { new() { renderer = renderer } };
        }
#endif
    }

    public enum EmoGesture
    {
        MenuOnly = -1,
        //Neutral = 0,
        Fist = 1,
        HandOpen = 2,
        FingerPoint = 3,
        Victory = 4,
        RockNRoll = 5,
        HandGun = 6,
        ThumbsUp = 7
    }

    public enum EmoHand
    {
        Any = 0,
        Left = 1,
        Right = 2
    }

    [Serializable]
    public class EmoCondition
    {
        public AnimatorControllerParameterType type = AnimatorControllerParameterType.Bool;
        public string parameter;
        public EmoConditionMode mode = EmoConditionMode.If;
        public float threshold;
    }

    public enum EmoConditionMode
    {
        If = 1,
        IfNot = 2,
        Greater = 3,
        Less = 4,
        Equals = 6,
        NotEqual = 7
    }

    [Serializable]
    public class EmoShape
    {
        public SkinnedMeshRenderer renderer;
        public EmoKey[] keys = new EmoKey[] { };
    }

    [Serializable]
    public class EmoKey
    {
        public string blendshape;
        public float value;
    }
}
