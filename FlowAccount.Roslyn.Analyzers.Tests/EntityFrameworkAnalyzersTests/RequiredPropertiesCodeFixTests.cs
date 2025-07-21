using System.Threading.Tasks;
using FlowAccount.Roslyn.Analyzers.EntityFrameworkAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FlowAccount.Roslyn.Analyzers.Tests.EntityFrameworkAnalyzersTests;

public class RequiredPropertiesCodeFixTests
{
    [Fact]
    public async Task AddsMissingRequiredPropertyToLambda()
    {
        var context = new CSharpCodeFixTest<RequiredPropertiesAnalyzer, RequiredPropertiesCodeFixProvider, DefaultVerifier>();
        context.CodeActionValidationMode = CodeActionValidationMode.Full;
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

    public interface IDataHandler<T>
        where T : class
    {
        IList<T> FindList(Expression<Func<T, bool>> where);
    }

    public class Model
    {
        [NotOptional]
        public int Id { get; set; }

        [NotOptional]
        public string Email { get; set; }

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

        context.FixedCode = """
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

    public interface IDataHandler<T>
        where T : class
    {
        IList<T> FindList(Expression<Func<T, bool>> where);
    }

    public class Model
    {
        [NotOptional]
        public int Id { get; set; }

        [NotOptional]
        public string Email { get; set; }

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
            _mockDataHandler.FindList(m => m.Name == "Test" && m.Id == /* FIXME: replace with actual value */ default && m.Email == /* FIXME: replace with actual value */ default);
        }
    }
}
""";

        await context.RunAsync();
    }
}
