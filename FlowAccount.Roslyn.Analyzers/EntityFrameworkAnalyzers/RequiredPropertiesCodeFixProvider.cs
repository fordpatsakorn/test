using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace FlowAccount.Roslyn.Analyzers.EntityFrameworkAnalyzers;

/// <summary>
/// Modifies lambda expression by appending missing required properties.
/// It detects properties with [NotOptional] on the underlying models.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequiredPropertiesCodeFixProvider))]
[Shared]
public class RequiredPropertiesCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add missing required properties to lambda";

    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("FAWRN0001");

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            var invocationExpr = node as InvocationExpressionSyntax ??
                                 node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocationExpr == null)
                continue;

            context.RegisterCodeFix(
                Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
                    Title,
                    c => AddMissingPropertiesAsync(context.Document, invocationExpr, c),
                    nameof(RequiredPropertiesCodeFixProvider)),
                diagnostic);
        }
    }

    private async Task<Document> AddMissingPropertiesAsync(Document document, InvocationExpressionSyntax invocationExpr,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;
        cancellationToken.ThrowIfCancellationRequested();

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var dataHandlerSymbol = semanticModel.Compilation.GetTypeByMetadataName("Flowaccount.Data.IDataHandler`1");
        if (dataHandlerSymbol == null)
            return document;

        var notOptionalSymbol =
            semanticModel.Compilation.GetTypeByMetadataName("FlowAccount.Core.Attributes.NotOptionalAttribute");
        if (notOptionalSymbol == null)
            return document;

        var methodSymbol = ModelExtensions.GetSymbolInfo(semanticModel, invocationExpr).Symbol as IMethodSymbol;
        if (methodSymbol == null) return document;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null ||
            containingType.TypeArguments.Length != 1 ||
            !SymbolEqualityComparer.Default.Equals(containingType.OriginalDefinition, dataHandlerSymbol))
            return document;

        var underlyingType = containingType.TypeArguments[0];
        if (underlyingType.TypeKind != TypeKind.Class)
            return document;

        var requiredProperties = underlyingType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, notOptionalSymbol)))
            .ToImmutableArray();

        var lambdaExpr = invocationExpr.ArgumentList.Arguments
            .Select(arg => arg.Expression)
            .OfType<LambdaExpressionSyntax>()
            .FirstOrDefault();

        if (lambdaExpr == null)
            return document;

        cancellationToken.ThrowIfCancellationRequested();
        var referencedSymbols = lambdaExpr.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Select(m => semanticModel.GetSymbolInfo(m).Symbol)
            .Where(s => s != null)
            .ToList();

        var missingProperties = requiredProperties
            .Where(p => !referencedSymbols.Contains(p))
            .ToList();

        if (missingProperties.Count == 0)
            return document;

        var parameterName = lambdaExpr switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 1 } parenthesized =>
                parenthesized.ParameterList.Parameters[0].Identifier.Text,
            _ => null
        };

        if (parameterName == null)
            return document;

        var missingExpressions = missingProperties.Select(p => SyntaxFactory.ParseExpression(
            $"{parameterName}.{p.Name} == /* FIXME: replace with actual value */ default")).ToArray();

        var originalBodyExpr = lambdaExpr.Body switch
        {
            ExpressionSyntax expr => expr,
            BlockSyntax block => block.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault()?.Expression,
            _ => null
        };
        if (originalBodyExpr == null)
            return document;

        // Combine all conditions into list without trivia to make it flat
        var allConditions = new[] { originalBodyExpr.WithoutTrivia() }
            .Concat(missingExpressions.Select(e => e.WithoutTrivia()))
            .ToList();
        ExpressionSyntax combinedCondition = allConditions.First();
        foreach (var expr in allConditions.Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            combinedCondition = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                combinedCondition,
                expr).WithTriviaFrom(originalBodyExpr);
        }

        var newLambdaExpr = lambdaExpr switch
        {
            SimpleLambdaExpressionSyntax simple =>
                simple.WithBody(combinedCondition).WithTriviaFrom(lambdaExpr),

            ParenthesizedLambdaExpressionSyntax parenthesized =>
                parenthesized.WithBody(combinedCondition).WithTriviaFrom(lambdaExpr),

            _ => lambdaExpr
        };
        editor.ReplaceNode(lambdaExpr, newLambdaExpr);
        return editor.GetChangedDocument();
    }
}
