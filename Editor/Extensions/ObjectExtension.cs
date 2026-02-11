using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace jp.lilxyzw.lilemo
{
    internal static partial class ObjHelper
    {
        internal static readonly Dictionary<GameObject, string> pathInAvatars = new();

        internal static T GetComponentInParentInRoot<T>(this GameObject gameObject, Transform root) where T : Object
        {
            var parent = gameObject.transform.parent;
            if (!parent) return null;
            var component = parent.GetComponent<T>();
            if (component) return component;
            if (parent == root) return null;
            return parent.gameObject.GetComponentInParentInRoot<T>(root);
        }

        internal static T GetComponentInParentInAvatar<T>(this GameObject gameObject) where T : Object
        {
            return gameObject.GetComponentInParentInRoot<T>(gameObject.GetAvatarRoot());
        }

        internal static Transform GetAvatarRoot(this GameObject gameObject)
        {
            //#if LIL_VRCSDK3A
            if (gameObject.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>()) return gameObject.transform;
            var descriptor = gameObject.GetComponentInParentInRoot<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(null);
            if (descriptor) return descriptor.transform;
            //#else
            //    if(gameObject.GetComponent<Animator>()) return gameObject.transform;
            //    var animator = gameObject.GetComponentInParentInRoot<Animator>(null);
            //    if(animator) return animator.transform;
            //#endif
            return null;
        }

        internal static Transform GetAvatarRoot(this Component component) => component.gameObject.GetAvatarRoot();

        // AnimationClip用のパス
        private static string GetPathFrom(this GameObject gameObject, Transform root)
        {
            var path = new StringBuilder();
            path.Append(gameObject.name);
            var parent = gameObject.transform.parent;
            while (parent && parent != root)
            {
                path.Insert(0, $"{parent.name}/");
                parent = parent.parent;
            }
            return path.ToString();
        }

        private static string GetPathInAvatar(this GameObject gameObject)
        {
            return gameObject.GetPathFrom(gameObject.GetAvatarRoot());
        }

        internal static string GetPathInAvatarFast(this GameObject gameObject)
        {
            if (pathInAvatars.ContainsKey(gameObject)) return pathInAvatars[gameObject];
            return pathInAvatars[gameObject] = gameObject.GetPathInAvatar();
        }

        internal static string GetPathInAvatarFast(this Component component)
        {
            return component.gameObject.GetPathInAvatarFast();
        }

        internal static bool TryGetRendererAndMeshWithBlendshape(Object obj, out SkinnedMeshRenderer renderer, out Mesh mesh)
        {
            renderer = null;
            mesh = null;
            if (obj is not SkinnedMeshRenderer s || s.sharedMesh is not Mesh m || m.blendShapeCount == 0) return false;
            renderer = s;
            mesh = m;
            return true;
        }
    }
}
