using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Anx.Analyzers.InheritanceProtection
{
    //[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InheritanceProtectionCodeFixProvider)), Shared]
    public class InheritanceProtectionCodeFixProvider : CodeFixProvider
    {
        private const string title = "Inherit from 'ViewModelBase'";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(InheritanceProtectionAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider() => null;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => InheritFromCorrectBaseClass(context.Document, root, declaration, c),
                    equivalenceKey: title),
                diagnostic
                );
        }

        private async Task<Document> InheritFromCorrectBaseClass(Document document, SyntaxNode root, TypeDeclarationSyntax typeDecl, CancellationToken c)
        {
            var semanticModel = await document.GetSemanticModelAsync(c);
            var currentBaseList = typeDecl.BaseList;
            var baseType = currentBaseList.Types
                .Where(x => (semanticModel.GetSymbolInfo(x.Type).Symbol as INamedTypeSymbol)?.IsInheritanceProtectionType() ?? false)
                .FirstOrDefault();
            
            if(baseType != null)
            {
                var correctBaseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Anx.Utility.ValidViewModelImpl"))
                    .WithLeadingTrivia(baseType.GetLeadingTrivia())
                    .WithTrailingTrivia(baseType.GetTrailingTrivia());
                
                var correctedBaseList = correctBaseType.AsEnumerable<BaseTypeSyntax>().Concat(currentBaseList.Types.Skip(1));
                var newBaseList = SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(correctedBaseList));
                var newRoot = root.ReplaceNode(currentBaseList, newBaseList);

                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        private async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            // Compute new uppercase name.
            var identifierToken = typeDecl.Identifier;
            var newName = identifierToken.Text.ToUpperInvariant();

            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            // Produce a new solution that has all references to that type renamed, including the declaration.
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, newName, optionSet, cancellationToken).ConfigureAwait(false);

            // Return the new solution with the now-uppercase type name.
            return newSolution;
        }
    }
}
