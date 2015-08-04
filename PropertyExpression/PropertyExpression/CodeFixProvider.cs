using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PropertyExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyExpressionCodeFixProvider)), Shared]
    public class PropertyExpressionCodeFixProvider : CodeFixProvider
    {
        private const string title = "Use nameof";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PropertyExpressionAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
                       
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            var expr = root.FindToken(diagnosticSpan.Start)
                .Parent.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => UseNameOfAsync(context.Document, expr, cancellationToken),
                    title),
                diagnostic);
        }

        private async Task<Document> UseNameOfAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var memberAccess = invocationExpression.Expression as MemberAccessExpressionSyntax;
            var genericName = memberAccess.Name as GenericNameSyntax;
            var identifierNameSyntax = (genericName.TypeArgumentList.Arguments[0] as IdentifierNameSyntax);
            string genericNameParamter = identifierNameSyntax.Identifier.ValueText;

            var childNodes = invocationExpression.ArgumentList.Arguments[0];
            var lambdaExp = childNodes.Expression as SimpleLambdaExpressionSyntax;
            var memberAccessBody = lambdaExp.Body as MemberAccessExpressionSyntax;

            // This is a clumsy way to get the full accessor path, maybe there is a more elegant solution
            string propertyName = memberAccessBody.ToString().Remove(0, lambdaExp.Parameter.Identifier.ValueText.Length + 1);

            var classDeclaration = invocationExpression.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var semanticModel = await document.GetSemanticModelAsync();
            var identifierSymbol = semanticModel.GetSymbolInfo(identifierNameSyntax).Symbol;
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            
            ExpressionSyntax nameOfSyntax;
            if (identifierSymbol.Equals(classSymbol))
            {
                nameOfSyntax = SyntaxFactory.ParseExpression($"nameof({propertyName})")
                    .WithLeadingTrivia(invocationExpression.GetLeadingTrivia())
                    .WithTrailingTrivia(invocationExpression.GetTrailingTrivia());
            }
            else
            {
                var propertyMembers = GetMemberHierarchy(classSymbol)
                    .Where(x => x.Kind == SymbolKind.Property)
                    .Select(x => ((IPropertySymbol)x).Name)
                    .ToList();
                
                string typeQualifier = genericNameParamter;
                if(propertyMembers.Contains(genericNameParamter))
                {
                    if(classSymbol.ContainingNamespace.Equals(identifierSymbol.ContainingNamespace))
                    {
                        typeQualifier = identifierSymbol.ContainingNamespace.Name + "." + typeQualifier;
                    }
                    else
                    {
                        typeQualifier = identifierSymbol.ContainingNamespace.ToString() + "." + typeQualifier;
                    }
                }

                nameOfSyntax = SyntaxFactory.ParseExpression($"nameof({typeQualifier}.{propertyName})")
                    .WithLeadingTrivia(invocationExpression.GetLeadingTrivia())
                    .WithTrailingTrivia(invocationExpression.GetTrailingTrivia());
            }

            var root = document.GetSyntaxRootAsync().Result;
            var newRoot = root.ReplaceNode(invocationExpression, nameOfSyntax);
            return document.WithSyntaxRoot(newRoot);
        }

        private ImmutableArray<ISymbol> GetMemberHierarchy(INamedTypeSymbol classSymbol)
        {
            var symbols = classSymbol.GetMembers();
            
            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                symbols = symbols.AddRange(baseType.GetMembers());
                baseType = baseType.BaseType;
            }

            return symbols;
        }
    }
}