using System.Threading.Tasks;
using FlowAccount.Roslyn.Analyzers.CommonAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FlowAccount.Roslyn.Analyzers.Tests.CommonAnalyzersTests;

public class TaskBlockingAnalyzerTests
{
    [Fact]
    public async Task UsageOfTaskWait_ShouldReportDiagnostic()
    {
        var context = new CSharpAnalyzerTest<TaskBlockingAnalyzer, DefaultVerifier>();
        context.TestCode = """
                           using System.Threading.Tasks;

                           class Program
                           {
                               void Example()
                               {
                                   Task task = Task.CompletedTask;
                                   {|FAINF0001:task.Wait|}();
                               }
                           }
                           """;
        await context.RunAsync();
    }

    [Fact]
    public async Task UsageOfTaskResult_ShouldReportDiagnostic()
    {
        var context = new CSharpAnalyzerTest<TaskBlockingAnalyzer, DefaultVerifier>();
        context.TestCode = """
                           using System.Threading.Tasks;

                           class Program
                           {
                               void Example()
                               {
                                   Task<int> task = Task.FromResult(42);
                                   var result = {|FAINF0001:task.Result|};
                               }
                           }
                           """;
        await context.RunAsync();
    }
}
