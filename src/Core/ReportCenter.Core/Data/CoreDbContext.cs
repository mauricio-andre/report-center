using Microsoft.EntityFrameworkCore;
using ReportCenter.Core.Reports.Entities;

namespace ReportCenter.Core.Data;

public abstract class CoreDbContext : DbContext
{
    protected CoreDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Report> Reports => Set<Report>();
}
