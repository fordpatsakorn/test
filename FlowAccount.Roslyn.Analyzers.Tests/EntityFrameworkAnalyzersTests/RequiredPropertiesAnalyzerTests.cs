using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using FlowAccount.Roslyn.Analyzers.EntityFrameworkAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Xunit;

namespace FlowAccount.Roslyn.Analyzers.Tests.EntityFrameworkAnalyzersTests;

public class RequiredPropertiesAnalyzerTests
{
    [Fact]
    public async Task QueryWithoutRequiredAttribute_ShouldNotReportDiagnostic()
    {
        var context = new CSharpAnalyzerTest<RequiredPropertiesAnalyzer, DefaultVerifier>();
        context.TestCode = """
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace FlowAccount.Core.Attributes
{
    public class NotOptionalAttribute : Attribute
    {
    }
}
namespace Flowaccount.Data
{
    using FlowAccount.Core.Attributes;
    public interface IDataHandler<T> where T : class
    {
        IList<T> FindList(Expression<Func<T, bool>> where);
    }

    public class Model
    {
        [NotOptional]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DataHandler<T> : IDataHandler<T> where T : class
    {
        public IList<T> FindList(Expression<Func<T, bool>> where)
        {

            return new List<T>().Where(where.Compile()).ToList();
        }
    }

    public class SampleService
    {
        private readonly IDataHandler<Model> _mockDataHandler = new DataHandler<Model>();
        public void SampleMethod()
        {
            _mockDataHandler.FindList(m => m.Id == 1 && m.Name == "Test");
        }
    }
}
""";
        await context.RunAsync();
    }

    [Fact]
    public async Task QueryWithoutRequiredAttribute_ShouldReportDiagnostic()
    {
        var context = new CSharpAnalyzerTest<RequiredPropertiesAnalyzer, DefaultVerifier>();
        context.TestCode = """
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;

namespace FlowAccount.Core.Attributes
{
    public class NotOptionalAttribute : Attribute
    {
    }
}

namespace Flowaccount.Data
{
    using FlowAccount.Core.Attributes;

    public interface IDataHandler<T> where T : class
    {
        IList<T> FindList(Expression<Func<T, bool>> where);
    }

    public class Model
    {
        [NotOptional]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DataHandler<T> : IDataHandler<T> where T : class
    {
        public IList<T> FindList(Expression<Func<T, bool>> where)
        {

            return new List<T>().Where(where.Compile()).ToList();
        }
    }

    public class SampleService
    {
        private readonly IDataHandler<Model> _mockDataHandler = new DataHandler<Model>();
        public void SampleMethod()
        {
            _mockDataHandler.{|FAWRN0001:FindList|}(m => m.Name == "Test");
        }
    }
}
""";
        await context.RunAsync();
    }
}
