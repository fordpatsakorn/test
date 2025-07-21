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

namespace test_app.TestCodeAnalyzer
{

    public class RawSql
    {
        private Microsoft.EntityFrameworkCore.DbContext _context;

        public RawSql(Microsoft.EntityFrameworkCore.DbContext context)
        {
            _context = context;
        }

        public void Execute()
        {
            _context.Set<Entity>().FromSqlRaw("SELECT * FROM Entity"); // Should have warning 
        }

    }
}