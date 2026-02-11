
using System.Collections.Generic;
using jp.lilxyzw.lilemo.runtime;
using nadena.dev.ndmf;
using UnityEngine;

namespace jp.lilxyzw.lilemo
{
    [ParameterProviderFor(typeof(Emo))]
    internal class EmoParameterProvider : IParameterProvider
    {
        private readonly Emo component;
        public EmoParameterProvider(Emo component) => this.component = component;
        public IEnumerable<ProvidedParameter> GetSuppliedParameters(BuildContext context = null)
        {
            yield return new ProvidedParameter(
                EmoProcessor.parameterName,
                ParameterNamespace.Animator,
                component,
                EmoPlugin.Instance,
                AnimatorControllerParameterType.Int
            ) {
                IsAnimatorOnly = false,
                IsHidden = false,
                WantSynced = true
            };
        }
    }
}
