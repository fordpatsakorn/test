using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FlowAccount.Roslyn.Analyzers.EntityFrameworkAnalyzers;

/// <summary>
/// Analyzer to validate lambda expression from IDataHandler method calls.
/// It checks for properties marked with [NotOptional]. If none of the
/// required properties are accessed, report a diagnostic.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiredPropertiesAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        "FAWRN0001",
        Resources.FAWRN0001Title,
        Resources.FAWRN0001MessageFormat,
        Resources.FAWRN0001Category,
        DiagnosticSeverity.Warning,
        true,
        Resources.FAWRN0001Description,
        Resources.FAWRN0001Link);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(c =>
        {
            var typeSymbol = c.Compilation.GetTypeByMetadataName("Flowaccount.Data.IDataHandler`1");
            if (typeSymbol == null) return;
            c.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;

        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpr).Symbol as IMethodSymbol;
        if (methodSymbol == null)
            return;

        var dataHandlerSymbol = context.Compilation.GetTypeByMetadataName("Flowaccount.Data.IDataHandler`1");
        if (dataHandlerSymbol == null)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null ||
            containingType.TypeArguments.Length != 1 ||
            !SymbolEqualityComparer.Default.Equals(containingType.OriginalDefinition, dataHandlerSymbol))
            return;

        var underlyingType = containingType.TypeArguments[0];
        if (underlyingType.TypeKind != TypeKind.Class)
            return;

        var notOptionalSymbol =
            context.Compilation.GetTypeByMetadataName("FlowAccount.Core.Attributes.NotOptionalAttribute");
        if (notOptionalSymbol == null)
            return;

        var lambdaExpr = invocationExpr.ArgumentList.Arguments
            .Select(arg => arg.Expression)
            .OfType<LambdaExpressionSyntax>()
            .FirstOrDefault();
        if (lambdaExpr == null)
            return;

        // Retrieved required properties from an underlying model
        var requiredProperties = underlyingType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>
                p.GetAttributes().Any(a =>
                    a.AttributeClass != null &&
                    SymbolEqualityComparer.Default.Equals(notOptionalSymbol, a.AttributeClass)
                ))
            .ToList();
        if (!requiredProperties.Any())
            return;

        // Do nothing if the lambda expression have the required properties
        var hasRequiredSymbol = lambdaExpr.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Select(m => context.SemanticModel.GetSymbolInfo(m).Symbol)
            .Any(symbol => symbol != null && requiredProperties.Contains(symbol,  SymbolEqualityComparer.Default));
        if (hasRequiredSymbol)
            return;

        var location = invocationExpr.Expression is MemberAccessExpressionSyntax member
            ? member.Name.GetLocation()
            : invocationExpr.GetLocation();
        var diagnostic = Diagnostic.Create(Rule, location, methodSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
