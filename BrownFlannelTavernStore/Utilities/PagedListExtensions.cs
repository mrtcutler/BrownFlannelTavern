using Microsoft.EntityFrameworkCore;

namespace BrownFlannelTavernStore.Utilities;

public static class PagedListExtensions
{
    public const int DefaultPageSize = 25;

    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> query, int page, int pageSize = DefaultPageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : pageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, page, pageSize, totalCount);
    }

    public static PagedList<T> ToPagedList<T>(
        this IEnumerable<T> source, int page, int pageSize = DefaultPageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : pageSize;

        var list = source as IList<T> ?? source.ToList();
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedList<T>(items, page, pageSize, list.Count);
    }
}
