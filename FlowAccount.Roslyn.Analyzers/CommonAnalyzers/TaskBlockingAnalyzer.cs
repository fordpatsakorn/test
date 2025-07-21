using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FlowAccount.Roslyn.Analyzers.CommonAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TaskBlockingAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "FAINF0001",
        Resources.FAINF0001Title,
        Resources.FAINF0001MessageFormat,
        Resources.FAINF0001Category,
        DiagnosticSeverity.Warning,
        true,
        Resources.FAINF0001Description,
        Resources.FAINF0001Link);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        // Task.Wait() is method invocation
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        // Task.Result is member access
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;
        var compilation = context.SemanticModel.Compilation;
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        if (node is InvocationExpressionSyntax invocationExpr)
        {
            if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (context.SemanticModel.GetSymbolInfo(memberAccess).Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Name == "Wait" &&
                    SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, taskType))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.GetLocation(), methodSymbol.Name));
                }
            }
        }
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;
        var compilation = context.SemanticModel.Compilation;
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

        if (node is MemberAccessExpressionSyntax memberAccessExpr)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(memberAccessExpr).Symbol;
            if (symbol is IPropertySymbol propertySymbol &&
                propertySymbol.Name == "Result" &&
                propertySymbol.ContainingType is INamedTypeSymbol containingType &&
                SymbolEqualityComparer.Default.Equals(containingType.OriginalDefinition, taskType))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccessExpr.GetLocation(), propertySymbol.Name));
            }
        }
    }
}
