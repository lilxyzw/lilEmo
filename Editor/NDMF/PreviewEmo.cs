using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using jp.lilxyzw.lilemo.runtime;
using nadena.dev.ndmf.preview;
using UnityEditor;
using UnityEngine;

namespace jp.lilxyzw.lilemo
{
    internal class PreviewAnythingReplacer : IRenderFilter
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            if (!context.GetComponentsByType<Emo>().Any()) return ImmutableList<RenderGroup>.Empty;
            return context.GetComponentsByType<SkinnedMeshRenderer>().Select(r => RenderGroup.For(r)).ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            foreach (var e in context.GetComponentsByType<Emo>())
            {
                context.Observe(e);
                if (e.clip) context.Observe(e.clip);
                if (e.GetComponent<EmoTransform>() is EmoTransform t) context.Observe(t);
            }
            context.Observe(proxyPairs.First().Item2);

            return Task.FromResult<IRenderFilterNode>(ShapeWeightNode.Instance);
        }

        private class ShapeWeightNode : IRenderFilterNode
        {
            public RenderAspects WhatChanged => RenderAspects.Shapes;
            public static readonly ShapeWeightNode Instance = new();
            private static Emo lastEmo;

            public void OnFrame(Renderer original, Renderer proxy)
            {
                if (!lastEmo || original is not SkinnedMeshRenderer origRenderer || !ObjHelper.TryGetRendererAndMeshWithBlendshape(proxy, out var renderer, out var mesh) || lastEmo.gameObject.GetAvatarRoot() != original.gameObject.GetAvatarRoot()) return;
                PreviewHelper.PreviewEmo(lastEmo, origRenderer, renderer, mesh);
            }

            [InitializeOnLoadMethod]
            private static void Init()
            {
                CheckSelectionChange();
                Selection.selectionChanged += CheckSelectionChange;
            }

            private static void CheckSelectionChange()
            {
                Emo emo = null;
                if (Selection.activeGameObject) emo = Selection.activeGameObject.GetComponent<Emo>();
                if ((emo || lastEmo) && emo != lastEmo)
                {
                    lastEmo = emo;
                    PreviewHelper.StartPreview(lastEmo);
                }
            }
        }
    }
}
