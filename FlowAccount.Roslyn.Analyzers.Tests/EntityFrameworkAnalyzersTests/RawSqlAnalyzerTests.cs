using System.Threading.Tasks;
using FlowAccount.Roslyn.Analyzers.EntityFrameworkAnalyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace FlowAccount.Roslyn.Analyzers.Tests.EntityFrameworkAnalyzersTests;

public class RawSqlAnalyzerTests
{
    private const string EfCoreStubs = """
namespace Microsoft.EntityFrameworkCore
{
    public class DbContext
    {
        public DbSet<TEntity> Set<TEntity>() => default;
    }
    public class DbSet<TEntity>
    {
        public void FromSqlRaw(string sql) { }
        public void SqlQueryRaw(string sql) { }
    }
}

public class Entity { }
""";

    [Fact]
    public async Task UsageOfFromSqlRaw_ShouldReportDiagnostic()
    {
        var context = new CSharpAnalyzerTest<RawSqlAnalyzer, DefaultVerifier>
        {
            TestCode = EfCoreStubs + """
                                  public class TestClass
                                  {
                                      private Microsoft.EntityFrameworkCore.DbContext _context;

                                      public TestClass(Microsoft.EntityFrameworkCore.DbContext context)
                                      {
                                          _context = context;
                                      }

                                      public void Execute()
                                      {
                                          _context.Set<Entity>().[|FromSqlRaw|]("SELECT * FROM Entity");
                                      }
                                  }
                                  """
        };
        await context.RunAsync();
    }


    [Fact]
    public async Task UsageOfFromSqlQueryRaw_ShouldReportDiagnostic()
    {
        var context = new CSharpAnalyzerTest<RawSqlAnalyzer, DefaultVerifier>
        {
            TestCode = EfCoreStubs + """
                                     public class TestClass
                                     {
                                         private Microsoft.EntityFrameworkCore.DbContext _context;

                                         public TestClass(Microsoft.EntityFrameworkCore.DbContext context)
                                         {
                                             _context = context;
                                         }

                                         public void Execute()
                                         {
                                             _context.Set<Entity>().[|SqlQueryRaw|]("SELECT * FROM Entity");
                                         }
                                     }
                                     """
        };
        await context.RunAsync();
    }

    [Fact]
    public async Task UsageOfNonEfCore_ShouldNotReportDiagnostic()
    {
        var context = new CSharpAnalyzerTest<RawSqlAnalyzer, DefaultVerifier>
        {
            TestCode = EfCoreStubs + """
                                     public class OtherLib
                                     {
                                         public class DbSet<T>
                                         {
                                             public void FromSqlRaw(string sql) { }
                                             public void SqlQueryRaw(string sql) { }
                                         }
                                     }

                                     public class AnotherClass
                                     {
                                         private OtherLib.DbSet<Entity> _set;

                                         public AnotherClass(OtherLib.DbSet<Entity> set)
                                         {
                                             _set = set;
                                         }

                                         public void Execute()
                                         {
                                             _set.FromSqlRaw("SELECT * FROM Entity");
                                             _set.SqlQueryRaw("SELECT * FROM Entity");
                                         }
                                     }
                                     """
        };
        await context.RunAsync();
    }
}
