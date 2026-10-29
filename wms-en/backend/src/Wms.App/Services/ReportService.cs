using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.Core.Common;
using Wms.Core.Utils;

namespace Wms.App.Services;

/// <summary>报表与工作台统计服务</summary>
public class ReportService
{
    private readonly WmsDbContext _db;
    private readonly InventoryQueryService _inventoryQuery;

    public ReportService(WmsDbContext db, InventoryQueryService inventoryQuery)
    {
        _db = db;
        _inventoryQuery = inventoryQuery;
    }

    /// <summary>库存汇总报表：按物料统计在库、锁定、可用与金额</summary>
    public async Task<List<StockSummaryItem>> StockSummaryAsync(long? warehouseId, CancellationToken cancellationToken = default)
    {
        var materials = await _db.Materials.ToListAsync(cancellationToken);
        var categories = await _db.MaterialCategories
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var recordSource = _db.InventoryRecords.AsQueryable();
        if (warehouseId.HasValue)
            recordSource = recordSource.Where(x => x.WarehouseId == warehouseId);

        var records = await recordSource.ToListAsync(cancellationToken);
        var items = new List<StockSummaryItem>();

        foreach (var material in materials)
        {
            var lines = records.Where(x => x.MaterialId == material.Id).ToList();
            if (lines.Count == 0)
                continue;

            var onHand = lines.Sum(x => x.OnHandQty);
            if (onHand == 0)
                continue;

            var locked = lines.Sum(x => x.LockedQty);
            var amount = lines.Sum(x => x.OnHandQty * x.UnitCost);

            items.Add(new StockSummaryItem
            {
                MaterialId = material.Id,
                Code = material.Code,
                Name = material.Name,
                Spec = material.Spec,
                Unit = material.Unit,
                CategoryName = categories.GetValueOrDefault(material.CategoryId),
                OnHandQty = onHand,
                LockedQty = locked,
                AvailableQty = onHand - locked,
                AvgCost = onHand > 0 ? Math.Round(amount / onHand, 2) : material.Price,
                Amount = Math.Round(amount, 2),
                SafetyStock = material.SafetyStock,
                Alert = onHand < material.SafetyStock
            });
        }

        return items.OrderByDescending(x => x.Amount).ToList();
    }

    /// <summary>收发存报表：期初 + 收入 - 发出 = 期末</summary>
    public async Task<List<InOutSummaryItem>> InOutSummaryAsync(
        string? startDate,
        string? endDate,
        long? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var end = DateRange.Parse(endDate) ?? DateTime.Today;
        var start = DateRange.Parse(startDate) ?? end.AddDays(-30);
        var endExclusive = DateRange.EndOfDayExclusive(end);

        var source = _db.InventoryTransactions.Where(x => x.OccurredAt < endExclusive);
        if (warehouseId.HasValue)
            source = source.Where(x => x.WarehouseId == warehouseId);

        var transactions = await source.ToListAsync(cancellationToken);
        if (transactions.Count == 0)
            return new List<InOutSummaryItem>();

        var materialIds = transactions.Select(x => x.MaterialId).Distinct().ToList();
        var materials = await _db.Materials
            .Where(x => materialIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var items = new List<InOutSummaryItem>();
        foreach (var materialId in materialIds)
        {
            var lines = transactions.Where(x => x.MaterialId == materialId).ToList();
            var beginQty = lines.Where(x => x.OccurredAt < start).Sum(x => x.Quantity);
            var inQty = lines.Where(x => x.OccurredAt >= start && x.Quantity > 0).Sum(x => x.Quantity);
            var outQty = -lines.Where(x => x.OccurredAt >= start && x.Quantity < 0).Sum(x => x.Quantity);
            var endQty = beginQty + inQty - outQty;

            if (beginQty == 0 && inQty == 0 && outQty == 0)
                continue;

            var material = materials.GetValueOrDefault(materialId);
            items.Add(new InOutSummaryItem
            {
                MaterialId = materialId,
                Code = material?.Code ?? string.Empty,
                Name = material?.Name ?? string.Empty,
                Unit = material?.Unit ?? string.Empty,
                BeginQty = beginQty,
                InQty = inQty,
                OutQty = outQty,
                EndQty = endQty
            });
        }

        return items.OrderBy(x => x.Code).ToList();
    }

    /// <summary>近 N 天出入库趋势</summary>
    public async Task<List<TrendItem>> TrendAsync(int days, CancellationToken cancellationToken = default)
    {
        var normalizedDays = Math.Clamp(days, 1, 90);
        var start = DateTime.Today.AddDays(-(normalizedDays - 1));
        var transactions = await _db.InventoryTransactions
            .Where(x => x.OccurredAt >= start)
            .ToListAsync(cancellationToken);

        var result = new List<TrendItem>();
        for (var i = 0; i < normalizedDays; i++)
        {
            var date = start.AddDays(i);
            var dayLines = transactions.Where(x => x.OccurredAt.Date == date).ToList();

            result.Add(new TrendItem
            {
                Date = date.ToString("yyyy-MM-dd"),
                InboundQty = dayLines.Where(x => x.Quantity > 0).Sum(x => x.Quantity),
                OutboundQty = -dayLines.Where(x => x.Quantity < 0).Sum(x => x.Quantity),
                InboundOrders = dayLines.Where(x => x.Type == InventoryTransactionType.Inbound && x.RefNo != null)
                    .Select(x => x.RefNo).Distinct().Count(),
                OutboundOrders = dayLines.Where(x => x.Type == InventoryTransactionType.Outbound && x.RefNo != null)
                    .Select(x => x.RefNo).Distinct().Count()
            });
        }

        return result;
    }

    /// <summary>出库量 TOP 物料</summary>
    public async Task<List<TopMaterialItem>> TopMaterialsAsync(int days, int count, CancellationToken cancellationToken = default)
    {
        var start = DateTime.Today.AddDays(-Math.Clamp(days, 1, 180));
        var take = Math.Clamp(count, 1, 50);

        var transactions = await _db.InventoryTransactions
            .Where(x => x.OccurredAt >= start && x.Quantity < 0)
            .ToListAsync(cancellationToken);

        var groups = transactions
            .GroupBy(x => x.MaterialId)
            .Select(g => new { MaterialId = g.Key, Qty = -g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Qty)
            .Take(take)
            .ToList();

        if (groups.Count == 0)
            return new List<TopMaterialItem>();

        var materialIds = groups.Select(x => x.MaterialId).ToList();
        var materials = await _db.Materials
            .Where(x => materialIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return groups.Select(g =>
        {
            var material = materials.GetValueOrDefault(g.MaterialId);
            return new TopMaterialItem
            {
                Code = material?.Code ?? string.Empty,
                Name = material?.Name ?? string.Empty,
                Unit = material?.Unit ?? string.Empty,
                OutQty = g.Qty,
                Amount = Math.Round(g.Qty * (material?.Price ?? 0), 2)
            };
        }).ToList();
    }

    /// <summary>工作台概览</summary>
    public async Task<DashboardDto> DashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var records = await _db.InventoryRecords.ToListAsync(cancellationToken);

        var dto = new DashboardDto
        {
            MaterialCount = await _db.Materials.CountAsync(x => x.Status == Core.Common.CommonStatus.Enabled, cancellationToken),
            WarehouseCount = await _db.Warehouses.CountAsync(x => x.Status == Core.Common.CommonStatus.Enabled, cancellationToken),
            SupplierCount = await _db.Suppliers.CountAsync(x => x.Status == Core.Common.CommonStatus.Enabled, cancellationToken),
            StockQty = records.Sum(x => x.OnHandQty),
            StockAmount = Math.Round(records.Sum(x => x.OnHandQty * x.UnitCost), 2),
            TodayInboundOrders = await _db.InboundOrders.CountAsync(x => x.CreatedAt >= today, cancellationToken),
            TodayOutboundOrders = await _db.OutboundOrders.CountAsync(x => x.CreatedAt >= today, cancellationToken),
            PendingInbound = await _db.InboundOrders.CountAsync(
                x => x.Status == Core.Common.OrderStatus.Draft || x.Status == Core.Common.OrderStatus.Confirmed, cancellationToken),
            PendingOutbound = await _db.OutboundOrders.CountAsync(
                x => x.Status == Core.Common.OrderStatus.Draft || x.Status == Core.Common.OrderStatus.Confirmed, cancellationToken)
        };

        var alerts = await _inventoryQuery.AlertsAsync(cancellationToken);
        dto.AlertCount = alerts.Count;
        dto.Trend = await TrendAsync(7, cancellationToken);
        dto.RecentTransactions = await _inventoryQuery.RecentAsync(8, cancellationToken);

        var materials = await _db.Materials.ToListAsync(cancellationToken);
        var categories = await _db.MaterialCategories.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var materialMap = materials.ToDictionary(x => x.Id);

        dto.CategoryDistribution = records
            .GroupBy(x => materialMap.GetValueOrDefault(x.MaterialId)?.CategoryId ?? 0)
            .Select(g => new DistributionItem
            {
                Name = categories.GetValueOrDefault(g.Key) ?? "Uncategorized",
                Value = Math.Round(g.Sum(x => x.OnHandQty * x.UnitCost), 2)
            })
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .ToList();

        return dto;
    }
}
