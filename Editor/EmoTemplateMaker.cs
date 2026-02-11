using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEngine;

namespace jp.lilxyzw.lilemo
{
    internal static class EmoTemplateMaker
    {
        private const string UNDO_ADD_TEMPLATE = "Add Template";
        private const string MENU_ROOT = "Tools/lilEmo/";
        private static Transform avatarRoot;

        [MenuItem(MENU_ROOT + "Template (Any Hand)")]
        private static void MakeTemplateAnyHand()
        {
            MakeEmoIdlePlaceholder();
            MakeEmoTemplate(EmoHand.Any);
        }

        [MenuItem(MENU_ROOT + "Template (Left and Right Hand)")]
        private static void MakeTemplateLeftAndRightHand()
        {
            MakeEmoIdlePlaceholder();
            MakeEmoTemplate(EmoHand.Left);
            MakeEmoTemplate(EmoHand.Right);
        }

        [MenuItem("Tools/lilEmo/Template (Any Hand)", true)]
        [MenuItem("Tools/lilEmo/Template (Left and Right Hand)", true)]
        private static bool CheckAvatar() => Selection.activeGameObject && (avatarRoot = Selection.activeGameObject.GetAvatarRoot());

        private static void MakeEmoIdlePlaceholder()
        {
            var obj = new GameObject("Idle");
            Undo.RegisterCreatedObjectUndo(obj, UNDO_ADD_TEMPLATE);
            Undo.SetTransformParent(obj.transform, avatarRoot, UNDO_ADD_TEMPLATE);
            Undo.AddComponent<EmoIdlePlaceholder>(obj);
        }

        private static void MakeEmo(EmoGesture gesture, EmoHand hand)
        {
            var obj = new GameObject(gesture.ToString() + (hand == EmoHand.Any ? "" : $" ({hand})"));
            Undo.RegisterCreatedObjectUndo(obj, UNDO_ADD_TEMPLATE);
            Undo.SetTransformParent(obj.transform, avatarRoot, UNDO_ADD_TEMPLATE);
            var emo = Undo.AddComponent<Emo>(obj);
            Undo.RecordObject(emo, UNDO_ADD_TEMPLATE);
            emo.gesture = gesture;
            emo.hand = hand;
        }

        private static void MakeEmoTemplate(EmoHand hand)
        {
            MakeEmo(EmoGesture.Fist, hand);
            MakeEmo(EmoGesture.HandOpen, hand);
            MakeEmo(EmoGesture.FingerPoint, hand);
            MakeEmo(EmoGesture.Victory, hand);
            MakeEmo(EmoGesture.RockNRoll, hand);
            MakeEmo(EmoGesture.HandGun, hand);
            MakeEmo(EmoGesture.ThumbsUp, hand);
        }
    }
}
