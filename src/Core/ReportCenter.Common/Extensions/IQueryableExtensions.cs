using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using ReportCenter.Common.Queries;


namespace ReportCenter.Common.Extensions;

public static class IQueryableExtensions
{
    public static IQueryable<TEntity> WhereIf<TEntity>(
        this IQueryable<TEntity> query,
        bool condition,
        Expression<Func<TEntity, bool>> predicate)
    {
        if (condition) return query.Where(predicate);

        return query;
    }

    public static IQueryable<TEntity> ApplyPagination<TEntity>(
        this IQueryable<TEntity> query,
        IPageableQuery request)
    {
        if (request.Skip.HasValue)
        {
            if (request.Skip < 0) throw new ArgumentOutOfRangeException("request.Skip", "Skip cannot be less than zero");

            query = query.Skip(request.Skip.Value);
        }

        if (request.Take.HasValue)
        {
            if (request.Take < 1) throw new ArgumentOutOfRangeException("request.Take", "Take cannot be less than one");

            query = query.Take(request.Take.Value);
        }

        return query;
    }

    public static IQueryable<TEntity> ApplySorting<TEntity>(
        this IQueryable<TEntity> query,
        ISortableQuery request,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? defaultSorting = null)
    {
        if (!string.IsNullOrEmpty(request?.SortBy)) return query.OrderBy(request.SortBy);
        if (defaultSorting != null) return defaultSorting(query);

        return query;
    }
}
