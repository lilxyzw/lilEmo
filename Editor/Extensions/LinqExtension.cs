using System;
using System.Collections.Generic;
using System.Linq;

namespace jp.lilxyzw.lilemo
{
    internal static class LinqExtension
    {
        public class InstantComparer<TSource, TKey> : IEqualityComparer<TSource>
        {
            private readonly Func<TSource, TKey> selector;
            public InstantComparer(Func<TSource, TKey> selector) => this.selector = selector;
            public bool Equals(TSource x, TSource y) => selector(x).Equals(selector(y));
            public int GetHashCode(TSource obj) => selector(obj).GetHashCode();
        }
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector) => source.Distinct(new InstantComparer<TSource, TKey>(selector));
    }
}
