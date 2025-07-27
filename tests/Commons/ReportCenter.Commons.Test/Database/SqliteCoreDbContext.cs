using Microsoft.EntityFrameworkCore;
using ReportCenter.Core.Data;
using ReportCenter.MongoDB.Data;

namespace ReportCenter.Commons.Test.Database;

public class SqliteCoreDbContext : MongoCoreDbContext
{
    private readonly SqliteConnectionPull _sqliteConnectionPull;
    public SqliteCoreDbContext(
        DbContextOptions<CoreDbContext> options,
        SqliteConnectionPull sqliteConnectionPull) : base(options)
    {
        _sqliteConnectionPull = sqliteConnectionPull;
    }

    // TODO configurar para mongoDB
    // protected override void UseTenantConnectionString(DbContextOptionsBuilder optionsBuilder, string connectionString)
    // {
    //     optionsBuilder.UseSqlite(_sqliteConnectionPull.GetOpenedConnection(connectionString));
    // }

}
