
namespace FinTrack.Application.Common;

public interface IPagedList
{
    int Page { get; }

    int PageSize { get; }

    int TotalCount { get; }

    int TotalPages { get; }
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount
) : IPagedList
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public int TotalPages =>
        TotalCount == 0
            ? 1
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static int NormalizePage(int page) =>
        page < 1 ? 1 : page;

    public static int NormalizePageSize(int pageSize) =>
        pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
