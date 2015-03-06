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

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
                       
            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

            var expr = root.FindToken(diagnosticSpan.Start)
                .Parent.AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create("Use nameof(...)",
                cancellationToken => UseNameOfAsync(context.Document, expr, cancellationToken)),
                diagnostic);
        }

        private Task<Document> UseNameOfAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var memberAccess = invocationExpression.Expression as MemberAccessExpressionSyntax;
            var genericName = memberAccess.Name as GenericNameSyntax;
            string genericNameParamter = (genericName.TypeArgumentList.Arguments[0] as IdentifierNameSyntax).Identifier.ValueText;

            var childNodes = invocationExpression.ArgumentList.Arguments[0];
            var lambdaExp = childNodes.Expression as SimpleLambdaExpressionSyntax;
            var memberAccessBody = lambdaExp.Body as MemberAccessExpressionSyntax;
            string propertyName = memberAccessBody.Name.Identifier.ValueText;

            var nameOfSyntax = SyntaxFactory.ParseExpression(string.Format("nameof({0}.{1})", genericNameParamter, propertyName))
                .WithLeadingTrivia(invocationExpression.GetLeadingTrivia())
                .WithTrailingTrivia(invocationExpression.GetTrailingTrivia());

            var root = document.GetSyntaxRootAsync().Result;
            var newRoot = root.ReplaceNode(invocationExpression, nameOfSyntax);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}