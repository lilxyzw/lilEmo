using System.Collections.Generic;
using jp.lilxyzw.lilemo.runtime;
using UnityEditor;
using UnityEngine;

namespace jp.lilxyzw.lilemo
{
    public partial class PreviewHelper : ScriptableSingleton<PreviewHelper>
    {
        /// <summary>
        /// NDMF Previewでアニメーション内容をプレビューする際に使用します。NDMF Previewで対応できないものはonAnimationModePreviewを使用してください。
        /// </summary>
        public static OnNDMFPreviewFunction onNDMFPreview;

        /// <summary>
        /// NDMF Previewでプレビューできない変更をプレビューする際に使用します。
        /// </summary>
        public static OnAnimationModePreviewFunction onAnimationModePreview;

        /// <summary>
        /// ビルド時に表情をキャプチャする際に使用します。現在はBlendShapeの変更にのみ対応しています。
        /// </summary>
        public static OnCaptureFunction onCapture;

        /// <summary>
        /// NDMF Previewでアニメーション内容をプレビューする際に使用します。NDMF Previewで対応できないものはonAnimationModePreviewを使用してください。
        /// </summary>
        /// <param name="emo">現在処理中のEmoコンポーネント</param>
        /// <param name="original">元のRenderer</param>
        /// <param name="proxy">プレビューに使用されるRenderer</param>
        public delegate void OnNDMFPreviewFunction(Emo emo, SkinnedMeshRenderer original, SkinnedMeshRenderer proxy);

        /// <summary>
        /// NDMF Previewでプレビューできない変更をプレビューする際に使用します。
        /// </summary>
        /// <param name="gameObject">選択中のGameObject</param>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        public delegate void OnAnimationModePreviewFunction(GameObject gameObject, GameObject avatarRoot);

        /// <summary>
        /// ビルド時に表情をキャプチャする際に使用します。現在はBlendShapeの変更にのみ対応しています。
        /// </summary>
        /// <param name="emo">現在処理中のEmoコンポーネント</param>
        /// <param name="avatarRoot">アバターのルートにあるGameObject</param>
        /// <param name="modifiedShapes">変更されたBlendShape</param>
        public delegate void OnCaptureFunction(Emo emo, GameObject avatarRoot, HashSet<(SkinnedMeshRenderer renderer, int index, float value)> modifiedShapes);

        /// <summary>
        /// onAnimationModePreviewを更新する際に使用します。
        /// </summary>
        /// <param name="obj">更新したコンポーネントまたはGameObject</param>
        public static void StartPreview(Object obj)
        {
            if (obj is GameObject gameObject) StartPreview(gameObject);
            else if (obj is Component component) StartPreview(component.gameObject);
            else AnimationMode.StopAnimationMode(Driver);
        }
    }
}
