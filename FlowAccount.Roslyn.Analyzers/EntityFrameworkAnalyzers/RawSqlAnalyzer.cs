using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FlowAccount.Roslyn.Analyzers.EntityFrameworkAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RawSqlAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "FAINF0002",
        Resources.FAINF0002Title,
        Resources.FAINF0002MessageFormat,
        Resources.FAINF0002Category,
        DiagnosticSeverity.Warning,
        true,
        Resources.FAINF0002Description,
        Resources.FAINF0002Link);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(c =>
        {
            c.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.InvocationExpression);
        });
    }
    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;
        var expression = invocationExpr.Expression;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(expression);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

        if (methodSymbol != null &&
            (methodSymbol.Name == "FromSqlRaw" || methodSymbol.Name == "SqlQueryRaw" || methodSymbol.Name == "ExecuteSqlRaw"))
        {
            var containingType = methodSymbol.ContainingType;
            var namespaceSymbol = containingType.ContainingNamespace;
            // Check if the methods are from `Microsoft.EntityFrameworkCore` namespace
            if (namespaceSymbol != null && namespaceSymbol.ToDisplayString() == "Microsoft.EntityFrameworkCore")
            {
                var location = expression is MemberAccessExpressionSyntax member
                    ? member.Name.GetLocation()
                    : invocationExpr.GetLocation();
                var diagnostic = Diagnostic.Create(Rule, location, methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
