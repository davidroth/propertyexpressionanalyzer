using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PropertyExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PropertyExpressionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PropertyExpression";
        
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        internal const string UtilTypeName = "PropertyUtil";
        internal const string UtilMethodName = "GetName";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpressionSyntax = (context.Node as InvocationExpressionSyntax);
            var memberAccess = invocationExpressionSyntax?.Expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                var name = (memberAccess.Expression as IdentifierNameSyntax)?.Identifier.ValueText;
                if (name == UtilTypeName)
                {
                    var childNodes = (context.Node as InvocationExpressionSyntax).ArgumentList.Arguments[0];
                    var lambdaExprSyntax = childNodes.Expression as SimpleLambdaExpressionSyntax;
                    if (lambdaExprSyntax == null)
                        return;

                    var memberAccessExprSyntax = lambdaExprSyntax.Body as MemberAccessExpressionSyntax;
                    if (memberAccessExprSyntax == null)
                        return;

                    string propertyName = memberAccessExprSyntax.Name.Identifier.ValueText;
                    var genericNameSyntax = memberAccess.Name as GenericNameSyntax;

                    var identifierSyntax = genericNameSyntax.TypeArgumentList.Arguments[0] as IdentifierNameSyntax;
                    if(identifierSyntax != null)
                    {
                        string genericNameParamter = identifierSyntax.Identifier.ValueText;

                        var diagnostic = Diagnostic.Create(Rule, memberAccess.GetLocation(), genericNameParamter, propertyName);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}