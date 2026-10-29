namespace Wms.Core.Common;

/// <summary>
/// 通用分页查询参数。
/// </summary>
public class PagedQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>页码，从 1 开始</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>每页条数，最大 200</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 200 ? 20 : value;
    }

    /// <summary>关键字模糊检索</summary>
    public string? Keyword { get; set; }

    /// <summary>计算跳过的记录数</summary>
    public int GetSkip() => (Page - 1) * PageSize;
}

/// <summary>
/// 通用分页结果。
/// </summary>
/// <typeparam name="T">列表项类型</typeparam>
public class PagedResult<T>
{
    /// <summary>当前页数据</summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>总记录数</summary>
    public long Total { get; set; }

    /// <summary>当前页码</summary>
    public int Page { get; set; }

    /// <summary>每页条数</summary>
    public int PageSize { get; set; }

    /// <summary>创建分页结果</summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, long total, int page, int pageSize)
        => new() { Items = items, Total = total, Page = page, PageSize = pageSize };
}
