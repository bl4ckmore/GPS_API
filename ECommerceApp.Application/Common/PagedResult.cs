namespace ECommerceApp.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public static PagedResult<T> Create(IEnumerable<T> items, int total, int page, int pageSize) =>
        new()
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
}
