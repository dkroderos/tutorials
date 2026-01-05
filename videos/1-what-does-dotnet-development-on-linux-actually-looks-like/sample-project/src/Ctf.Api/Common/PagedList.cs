using Dapper;
using Npgsql;

namespace Ctf.Api.Common;

public sealed class PagedList<T>
{
    private PagedList(List<T> items, int currentPage, int pageSize, long totalCount)
    {
        Items = items;
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalItems = totalCount;
    }

    public List<T> Items { get; set; }
    public int CurrentPage { get; }
    public int PageSize { get; }
    public long TotalItems { get; set; }
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasPreviousPage => CurrentPage > 1;
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public static PagedList<T> Create(List<T> items, int currentPage, int pageSize, long totalItems)
    {
        return new PagedList<T>(items, currentPage, pageSize, totalItems);
    }

    public static PagedList<T> Empty()
    {
        return new PagedList<T>([], currentPage: 1, pageSize: 0, totalCount: 0);
    }

    public static async Task<PagedList<T>> CreateAsync(
        NpgsqlDataSource dataSource,
        string baseSelectSql,
        string sortColumn,
        bool isAscending,
        int page,
        int pageSize,
        object parameters
    )
    {
        var sql = $"""
            WITH filtered AS (
                {baseSelectSql}
            )
            SELECT *
            FROM filtered
            ORDER BY {sortColumn} {isAscending.ToSortOrderSql()}
            LIMIT @PageSize OFFSET @Offset;

            WITH filtered AS (
                {baseSelectSql}
            )
            SELECT COUNT(*) FROM filtered;
            """;

        var dynParams = new DynamicParameters(parameters);
        dynParams.Add("Page", page);
        dynParams.Add("PageSize", pageSize);
        dynParams.Add("Offset", (page - 1) * pageSize);

        await using var db = await dataSource.OpenConnectionAsync();
        await using var multi = await db.QueryMultipleAsync(sql, dynParams);

        var items = (await multi.ReadAsync<T>()).ToList();
        var totalItems = await multi.ReadSingleAsync<int>();

        return new PagedList<T>(items, page, pageSize, totalItems);
    }
}
