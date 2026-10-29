using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.Core.Common;
using Wms.Core.Utils;
using Wms.Im.Entities;

namespace Wms.App.Services;

/// <summary>库存查询服务：台账、流水、预警与仓库汇总</summary>
public class InventoryQueryService
{
    private readonly WmsDbContext _db;

    public InventoryQueryService(WmsDbContext db)
    {
        _db = db;
    }

    /// <summary>分页查询库存台账</summary>
    public async Task<PagedResult<InventoryItemDto>> QueryAsync(InventoryQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.InventoryRecords
            .Join(_db.Materials, r => r.MaterialId, m => m.Id, (r, m) => new { r, m })
            .Join(_db.Warehouses, x => x.r.WarehouseId, w => w.Id, (x, w) => new { x.r, x.m, w })
            .Join(_db.WarehouseLocations, x => x.r.LocationId, l => l.Id, (x, l) => new { x.r, x.m, x.w, l });

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.m.Code.Contains(keyword)
                                       || x.m.Name.Contains(keyword)
                                       || x.r.BatchNo.Contains(keyword)
                                       || x.l.Code.Contains(keyword));
        }
        if (query.WarehouseId.HasValue) source = source.Where(x => x.r.WarehouseId == query.WarehouseId);
        if (query.LocationId.HasValue) source = source.Where(x => x.r.LocationId == query.LocationId);
        if (query.MaterialId.HasValue) source = source.Where(x => x.r.MaterialId == query.MaterialId);
        if (query.OnlyInStock == true) source = source.Where(x => x.r.OnHandQty > 0);
        if (query.OnlyAlerts == true) source = source.Where(x => x.r.OnHandQty < x.m.SafetyStock);

        var total = await source.LongCountAsync(cancellationToken);
        var rows = await source
            .OrderBy(x => x.m.Code).ThenBy(x => x.w.Code).ThenBy(x => x.l.Code)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new InventoryItemDto
        {
            Id = x.r.Id,
            MaterialId = x.r.MaterialId,
            MaterialCode = x.m.Code,
            MaterialName = x.m.Name,
            Spec = x.m.Spec,
            Unit = x.m.Unit,
            WarehouseId = x.r.WarehouseId,
            WarehouseName = x.w.Name,
            LocationId = x.r.LocationId,
            LocationCode = x.l.Code,
            BatchNo = x.r.BatchNo,
            OnHandQty = x.r.OnHandQty,
            LockedQty = x.r.LockedQty,
            AvailableQty = x.r.OnHandQty - x.r.LockedQty,
            UnitCost = x.r.UnitCost,
            Amount = x.r.OnHandQty * x.r.UnitCost,
            SafetyStock = x.m.SafetyStock,
            Alert = x.r.OnHandQty < x.m.SafetyStock,
            LastInAt = x.r.LastInAt,
            LastOutAt = x.r.LastOutAt
        }).ToList();

        return PagedResult<InventoryItemDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>分页查询库存流水</summary>
    public async Task<PagedResult<TransactionDto>> TransactionsAsync(TransactionQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.InventoryTransactions
            .Join(_db.Materials, t => t.MaterialId, m => m.Id, (t, m) => new { t, m })
            .Join(_db.Warehouses, x => x.t.WarehouseId, w => w.Id, (x, w) => new { x.t, x.m, w })
            .Join(_db.WarehouseLocations, x => x.t.LocationId, l => l.Id, (x, l) => new { x.t, x.m, x.w, l });

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.t.No.Contains(keyword)
                                       || x.m.Code.Contains(keyword)
                                       || x.m.Name.Contains(keyword)
                                       || (x.t.RefNo != null && x.t.RefNo.Contains(keyword)));
        }
        if (query.Type.HasValue) source = source.Where(x => x.t.Type == query.Type);
        if (query.MaterialId.HasValue) source = source.Where(x => x.t.MaterialId == query.MaterialId);
        if (query.WarehouseId.HasValue) source = source.Where(x => x.t.WarehouseId == query.WarehouseId);
        if (!string.IsNullOrWhiteSpace(query.RefNo)) source = source.Where(x => x.t.RefNo == query.RefNo);

        var start = DateRange.Parse(query.StartDate);
        if (start.HasValue) source = source.Where(x => x.t.OccurredAt >= start.Value);
        var end = DateRange.Parse(query.EndDate);
        if (end.HasValue) source = source.Where(x => x.t.OccurredAt < DateRange.EndOfDayExclusive(end.Value));

        var total = await source.LongCountAsync(cancellationToken);
        var rows = await source
            .OrderByDescending(x => x.t.OccurredAt).ThenByDescending(x => x.t.Id)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new TransactionDto
        {
            Id = x.t.Id,
            No = x.t.No,
            Type = x.t.Type,
            MaterialId = x.t.MaterialId,
            MaterialCode = x.m.Code,
            MaterialName = x.m.Name,
            Unit = x.m.Unit,
            WarehouseName = x.w.Name,
            LocationCode = x.l.Code,
            BatchNo = x.t.BatchNo,
            Quantity = x.t.Quantity,
            LockedQuantity = x.t.LockedQuantity,
            BalanceQty = x.t.BalanceQty,
            RefType = x.t.RefType,
            RefNo = x.t.RefNo,
            Remark = x.t.Remark,
            OccurredAt = x.t.OccurredAt,
            OperatorName = x.t.OperatorName
        }).ToList();

        return PagedResult<TransactionDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>按仓库汇总库存</summary>
    public async Task<List<InventorySummaryDto>> SummaryAsync(CancellationToken cancellationToken = default)
    {
        var records = await _db.InventoryRecords.ToListAsync(cancellationToken);
        var warehouses = await _db.Warehouses.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return warehouses.Select(pair =>
        {
            var lines = records.Where(x => x.WarehouseId == pair.Key).ToList();
            return new InventorySummaryDto
            {
                WarehouseId = pair.Key,
                WarehouseName = pair.Value,
                SkuCount = lines.Select(x => x.MaterialId).Distinct().Count(),
                OnHandQty = lines.Sum(x => x.OnHandQty),
                LockedQty = lines.Sum(x => x.LockedQty),
                AvailableQty = lines.Sum(x => x.OnHandQty - x.LockedQty),
                Amount = lines.Sum(x => x.OnHandQty * x.UnitCost)
            };
        }).OrderByDescending(x => x.Amount).ToList();
    }

    /// <summary>库存预警：按物料 + 仓库汇总后与安全库存比较</summary>
    public async Task<List<AlertItemDto>> AlertsAsync(CancellationToken cancellationToken = default)
    {
        var materials = await _db.Materials
            .Where(x => x.SafetyStock > 0)
            .ToListAsync(cancellationToken);
        if (materials.Count == 0) return new List<AlertItemDto>();

        var records = await _db.InventoryRecords.ToListAsync(cancellationToken);
        var warehouses = await _db.Warehouses.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var stockByKey = records
            .GroupBy(x => new { x.MaterialId, x.WarehouseId })
            .ToDictionary(
                g => (g.Key.MaterialId, g.Key.WarehouseId),
                g => new { OnHand = g.Sum(x => x.OnHandQty), Locked = g.Sum(x => x.LockedQty) });

        var alerts = new List<AlertItemDto>();
        foreach (var material in materials)
        {
            foreach (var warehouse in warehouses)
            {
                var stock = stockByKey.GetValueOrDefault((material.Id, warehouse.Key));
                var onHand = stock?.OnHand ?? 0;
                if (onHand >= material.SafetyStock)
                    continue;

                alerts.Add(new AlertItemDto
                {
                    MaterialId = material.Id,
                    MaterialCode = material.Code,
                    MaterialName = material.Name,
                    Spec = material.Spec,
                    Unit = material.Unit,
                    WarehouseId = warehouse.Key,
                    WarehouseName = warehouse.Value,
                    OnHandQty = onHand,
                    LockedQty = stock?.Locked ?? 0,
                    SafetyStock = material.SafetyStock,
                    ShortageQty = material.SafetyStock - onHand
                });
            }
        }

        return alerts
            .OrderBy(x => x.OnHandQty - x.SafetyStock)
            .ToList();
    }

    /// <summary>查询最近库存流水（工作台使用）</summary>
    public async Task<List<TransactionDto>> RecentAsync(int take, CancellationToken cancellationToken = default)
    {
        var result = await TransactionsAsync(new TransactionQuery { Page = 1, PageSize = Math.Clamp(take, 1, 50) }, cancellationToken);
        return result.Items.ToList();
    }
}
