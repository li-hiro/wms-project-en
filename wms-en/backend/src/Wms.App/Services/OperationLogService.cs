using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Utils;

namespace Wms.App.Services;

/// <summary>操作日志服务</summary>
public class OperationLogService
{
    private readonly WmsDbContext _db;

    public OperationLogService(WmsDbContext db)
    {
        _db = db;
    }

    /// <summary>分页查询操作日志</summary>
    public async Task<PagedResult<LogDto>> QueryAsync(LogQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.OperationLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Module))
            source = source.Where(x => x.Module == query.Module);

        if (query.Success.HasValue)
            source = source.Where(x => x.Success == query.Success);

        var start = DateRange.Parse(query.StartDate);
        if (start.HasValue)
            source = source.Where(x => x.CreatedAt >= start.Value);

        var end = DateRange.Parse(query.EndDate);
        if (end.HasValue)
            source = source.Where(x => x.CreatedAt < DateRange.EndOfDayExclusive(end.Value));

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => (x.UserName != null && x.UserName.Contains(keyword))
                                       || x.Action.Contains(keyword)
                                       || x.Path.Contains(keyword));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderByDescending(x => x.Id)
            .Skip(query.GetSkip())
            .Take(query.PageSize)
            .Select(x => new LogDto
            {
                Id = x.Id,
                UserName = x.UserName,
                Module = x.Module,
                Action = x.Action,
                Method = x.Method,
                Path = x.Path,
                Ip = x.Ip,
                Params = x.Params,
                Success = x.Success,
                DurationMs = x.DurationMs,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<LogDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>写入一条操作日志</summary>
    public async Task WriteAsync(OperationLog log, CancellationToken cancellationToken = default)
    {
        _db.OperationLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>清理指定天数之前的日志</summary>
    public async Task<int> ClearAsync(int keepDays, CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.Now.AddDays(-Math.Abs(keepDays));
        var expired = await _db.OperationLogs.Where(x => x.CreatedAt < threshold).ToListAsync(cancellationToken);
        _db.OperationLogs.RemoveRange(expired);
        await _db.SaveChangesAsync(cancellationToken);
        return expired.Count;
    }
}
