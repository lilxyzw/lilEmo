using System;
using UnityEngine;

namespace jp.lilxyzw.lilemo.runtime
{
    public class EmoTransform : EmoEditorOnly
    {
        public EmoTransformSetting[] transforms;
    }

    [Serializable]
    public class EmoTransformSetting
    {
        public Transform transform;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }
}
