using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anx.Analyzers.InheritanceProtection
{
    internal static class NamedTypeSymbolExtensions
    {
        public static bool IsInheritanceProtectionType(this INamedTypeSymbol type)
        {
            return type.ContainingAssembly.Name.Equals("Anx.Utility", StringComparison.Ordinal)
                && type.ContainingNamespace.ToDisplayString().Equals("Anx.Utility", StringComparison.Ordinal);
        }
    }
}
