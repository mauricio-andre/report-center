using Microsoft.EntityFrameworkCore;
using ReportCenter.Core.Data;
using ReportCenter.MongoDB.Configurations.Reports;

namespace ReportCenter.MongoDB.Data;

public class MongoCoreDbContext : CoreDbContext
{
    public MongoCoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ReportEfConfiguration());
    }
}
