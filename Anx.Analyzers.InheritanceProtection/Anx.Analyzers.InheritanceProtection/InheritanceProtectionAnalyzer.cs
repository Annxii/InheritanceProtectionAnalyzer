using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Anx.Analyzers.InheritanceProtection
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InheritanceProtectionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ANX1000";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Inheritance";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            var baseTypeSymbol = namedTypeSymbol.BaseType;

            if(baseTypeSymbol != null && baseTypeSymbol.BaseType != null && !baseTypeSymbol.Name.Equals("Attribute", StringComparison.Ordinal))
            {
                if(baseTypeSymbol.GetAttributes().Any(IsInheritanceProtectionAttribute))
                {
                    if (!namedTypeSymbol.IsInheritanceProtectionType())
                    {
                        var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name, baseTypeSymbol.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool IsInheritanceProtectionAttribute(AttributeData attr)
        {
            var type = attr.AttributeClass;
            if(type.Name.Equals("InheritanceProtectionAttribute", StringComparison.Ordinal))
            {
                return type.IsInheritanceProtectionType();
            }

            return false;
        }
    }
}
