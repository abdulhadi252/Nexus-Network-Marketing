using Microsoft.EntityFrameworkCore;

namespace Nexos_Network_System.Helpers;

public abstract class PagedResultBase
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public class PagedResult<T> : PagedResultBase
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> query, int page, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}
