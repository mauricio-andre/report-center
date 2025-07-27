using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReportCenter.Core.Data;

namespace ReportCenter.MongoDB.Data;

public class MongoCoreDbContextFactory : IDbContextFactory<CoreDbContext>
{
    private readonly IServiceProvider _serviceProvider;

    public MongoCoreDbContextFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public CoreDbContext CreateDbContext() => ActivatorUtilities.CreateInstance<MongoCoreDbContext>(_serviceProvider);

    public Task<CoreDbContext> CreateDbContextAsync() => Task.FromResult(CreateDbContext());
}
