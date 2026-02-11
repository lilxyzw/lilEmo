using jp.lilxyzw.lilemo;
using jp.lilxyzw.lilemo.runtime;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(EmoPlugin))]

namespace jp.lilxyzw.lilemo
{
    internal class EmoPlugin : Plugin<EmoPlugin>
    {
        public override string QualifiedName => "jp.lilxyzw.lilemo";
        public override string DisplayName => "lilEmo";

        protected override void Configure()
        {
            var Generating = InPhase(BuildPhase.Generating).BeforePlugin("nadena.dev.modular-avatar").AfterPlugin("net.rs64.tex-trans-tool");
            Generating.Run("Generate Animations and MA Components", ctx =>
            {
                if (!ctx.AvatarRootObject.GetComponentInChildren<Emo>(true)) return;

                var settings = ctx.AvatarRootObject.GetComponentInChildren<EmoSettings>(true);
                var icons = PreviewHelper.Capture(ctx.AvatarRootObject, settings);
                VRChatModule.Setup(ctx.AvatarRootObject, out var blinkBinding);

                // MA用一時オブジェクト
                var tempObject = new GameObject("Expressions");
                tempObject.transform.parent = ctx.AvatarRootTransform;

                ModularAvatarMergeAnimator maAnimator;
                if (settings && settings.gameObject.GetComponent<ModularAvatarMergeAnimator>() is ModularAvatarMergeAnimator mergeAnimator)
                {
                    maAnimator = mergeAnimator;
                }
                else
                {
                    maAnimator = tempObject.AddComponent<ModularAvatarMergeAnimator>();
                }
                maAnimator.pathMode = MergeAnimatorPathMode.Absolute;

                tempObject.AddComponent<ModularAvatarParameters>().parameters.Add(new()
                {
                    nameOrPrefix = EmoProcessor.parameterName,
                    internalParameter = false,
                    isPrefix = false,
                    syncType = ParameterSyncType.Int,
                    localOnly = false,
                    defaultValue = 0,
                    saved = false,
                });

                // コンポーネントをAnimatorControllerに変換
                // ついでにメニュー追加
                bool containsRootMenu = false;
                var controller = EmoProcessor.Process(ctx.AvatarRootObject, settings, blinkBinding, (emo, i) =>
                {
                    GameObject gameObject = null;
                    if (emo) gameObject = emo.gameObject;
                    else if (ctx.AvatarRootObject.GetComponentInChildren<EmoIdlePlaceholder>() is EmoIdlePlaceholder placeholder) gameObject = placeholder.gameObject;

                    if (!gameObject || !gameObject.GetComponentInParent<ModularAvatarMenuInstaller>())
                    {
                        if(emo) gameObject = new GameObject(gameObject.name);
                        else gameObject = new GameObject("Idle");
                        gameObject.transform.parent = tempObject.transform;
                        containsRootMenu = true;
                    }

                    var item = gameObject.AddComponent<ModularAvatarMenuItem>();
                    item.label = gameObject.name;
                    item.PortableControl.Type = PortableControlType.Toggle;
                    item.PortableControl.Parameter = EmoProcessor.parameterName;
                    item.PortableControl.Value = i;
                    if (emo) item.PortableControl.Icon = icons[emo];
                });
                maAnimator.animator = controller;

                // MenuInstaller配下にないEmoがあった場合はメニューを作成
                if (containsRootMenu)
                {
                    var maMenuInstaller = tempObject.AddComponent<ModularAvatarMenuInstaller>();
                    maMenuInstaller.installTargetMenu = VRChatModule.GetDescriptor(ctx.AvatarRootObject).expressionsMenu;

                    var maMenuItem = tempObject.AddComponent<ModularAvatarMenuItem>();
                    maMenuItem.PortableControl.Type = PortableControlType.SubMenu;
                    maMenuItem.MenuSource = SubmenuSource.Children;
                }

                // ダンスワールド対応
                controller.layers[0].stateMachine.AddStateMachineBehaviour<ModularAvatarMMDLayerControl>().DisableInMMDMode = true;
                controller.layers[1].stateMachine.AddStateMachineBehaviour<ModularAvatarMMDLayerControl>().DisableInMMDMode = true;
            }).PreviewingWith(new PreviewAnythingReplacer());

            Generating.Run("Remove Components", ctx =>
            {
                foreach (var e in ctx.AvatarRootObject.GetComponentsInChildren<EmoEditorOnly>(true)) Object.DestroyImmediate(e);
            });
        }
    }
}
