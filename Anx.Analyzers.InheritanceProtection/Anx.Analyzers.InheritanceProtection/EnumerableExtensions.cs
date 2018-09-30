using System;
using System.Collections.Generic;
using System.Text;

namespace Anx.Analyzers.InheritanceProtection
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this T element)
        {
            yield return element;
        }
    }
}
