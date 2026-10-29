using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Im;
using Wms.Im.Entities;
using Wms.Core.Common;
using Wms.Core.Utils;

namespace Wms.App.Services;

/// <summary>库存盘点服务：账面快照、实盘录入与差异调整</summary>
public class StockTakeService
{
    private readonly WmsDbContext _db;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly InventoryManager _inventory;
    private readonly ICurrentUser _currentUser;

    public StockTakeService(
        WmsDbContext db,
        IInventoryRepository inventoryRepository,
        InventoryManager inventory,
        ICurrentUser currentUser)
    {
        _db = db;
        _inventoryRepository = inventoryRepository;
        _inventory = inventory;
        _currentUser = currentUser;
    }

    /// <summary>分页查询盘点单</summary>
    public async Task<PagedResult<StockTakeDto>> QueryAsync(StockTakeQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.StockTakeOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.OrderNo.Contains(keyword));
        }
        if (query.Status.HasValue) source = source.Where(x => x.Status == query.Status);
        if (query.WarehouseId.HasValue) source = source.Where(x => x.WarehouseId == query.WarehouseId);

        var total = await source.LongCountAsync(cancellationToken);
        var orders = await source
            .OrderByDescending(x => x.Id)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(x => x.Id).ToList();
        var items = await _db.StockTakeItems.Where(x => orderIds.Contains(x.OrderId)).ToListAsync(cancellationToken);

        var warehouseIds = orders.Select(x => x.WarehouseId).Distinct().ToList();
        var warehouses = await _db.Warehouses
            .Where(x => warehouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var result = orders.Select(order =>
        {
            var lines = items.Where(x => x.OrderId == order.Id).ToList();
            return new StockTakeDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                WarehouseId = order.WarehouseId,
                WarehouseName = warehouses.GetValueOrDefault(order.WarehouseId),
                Status = order.Status,
                TakeDate = order.TakeDate,
                Remark = order.Remark,
                CreatedByName = order.CreatedByName,
                CreatedAt = order.CreatedAt,
                ItemCount = lines.Count,
                DiffCount = lines.Count(x => x.CountedQty != x.SystemQty)
            };
        }).ToList();

        return PagedResult<StockTakeDto>.Create(result, total, query.Page, query.PageSize);
    }

    /// <summary>获取盘点单详情</summary>
    public async Task<StockTakeDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.StockTakeOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Stocktake order not found");

        var warehouse = await _db.Warehouses.FindAsync(new object[] { order.WarehouseId }, cancellationToken);
        var lines = order.Items.ToList();

        var materialIds = lines.Select(x => x.MaterialId).Distinct().ToList();
        var materials = await _db.Materials
            .Where(x => materialIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code, x.Name, x.Unit })
            .ToListAsync(cancellationToken);

        var locationIds = lines.Select(x => x.LocationId).Distinct().ToList();
        var locations = await _db.WarehouseLocations
            .Where(x => locationIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code })
            .ToListAsync(cancellationToken);

        return new StockTakeDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            WarehouseId = order.WarehouseId,
            WarehouseName = warehouse?.Name,
            Status = order.Status,
            TakeDate = order.TakeDate,
            Remark = order.Remark,
            CreatedByName = order.CreatedByName,
            CreatedAt = order.CreatedAt,
            ItemCount = lines.Count,
            DiffCount = lines.Count(x => x.CountedQty != x.SystemQty),
            Items = lines.Select(item =>
            {
                var material = materials.FirstOrDefault(x => x.Id == item.MaterialId);
                return new StockTakeItemDto
                {
                    Id = item.Id,
                    MaterialId = item.MaterialId,
                    MaterialCode = material?.Code ?? string.Empty,
                    MaterialName = material?.Name ?? string.Empty,
                    Unit = material?.Unit ?? string.Empty,
                    LocationId = item.LocationId,
                    LocationCode = locations.FirstOrDefault(x => x.Id == item.LocationId)?.Code,
                    BatchNo = item.BatchNo,
                    SystemQty = item.SystemQty,
                    CountedQty = item.CountedQty,
                    DiffQty = item.CountedQty - item.SystemQty
                };
            }).ToList()
        };
    }

    /// <summary>创建盘点单（按当前库存自动生成账面快照）</summary>
    public async Task<long> CreateAsync(CreateStockTakeRequest request, CancellationToken cancellationToken = default)
    {
        var warehouseExists = await _db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException("Stocktake warehouse not found");

        var records = await _inventoryRepository.ListByWarehouseAsync(request.WarehouseId, cancellationToken);
        if (records.Count == 0)
            throw new BusinessException("No stock records available for stocktake in this warehouse");

        var order = new StockTakeOrder
        {
            OrderNo = OrderNoGenerator.Generate("TK"),
            WarehouseId = request.WarehouseId,
            Status = StockTakeStatus.Draft,
            TakeDate = request.TakeDate?.Date ?? DateTime.Today,
            Remark = request.Remark,
            CreatedBy = _currentUser.UserId,
            CreatedByName = _currentUser.NickName ?? _currentUser.UserName
        };

        foreach (var record in records)
        {
            order.Items.Add(new StockTakeItem
            {
                MaterialId = record.MaterialId,
                InventoryRecordId = record.Id,
                LocationId = record.LocationId,
                BatchNo = record.BatchNo,
                SystemQty = record.OnHandQty,
                CountedQty = record.OnHandQty
            });
        }

        _db.StockTakeOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    /// <summary>录入实盘数量（仅草稿状态）</summary>
    public async Task UpdateItemsAsync(long id, UpdateStockTakeItemsRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _db.StockTakeOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Stocktake order not found");

        if (order.Status != StockTakeStatus.Draft)
            throw new BusinessException("Only pending stocktake orders can have counted quantities entered", ErrorCodes.InvalidStatus);

        var itemMap = order.Items.ToDictionary(x => x.Id);
        foreach (var entry in request.Items)
        {
            if (!itemMap.TryGetValue(entry.Id, out var item))
                continue;

            if (entry.CountedQty < 0)
                throw new BusinessException("Counted quantity cannot be negative");

            item.CountedQty = entry.CountedQty;
        }

        order.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>完成盘点并按差异调整库存</summary>
    public async Task CompleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.StockTakeOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Stocktake order not found");

        if (order.Status != StockTakeStatus.Draft)
            throw new BusinessException("This stocktake order is already completed or cancelled", ErrorCodes.InvalidStatus);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            var adjust = await _inventory.AdjustAsync(new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = order.WarehouseId,
                LocationId = item.LocationId,
                BatchNo = item.BatchNo,
                Quantity = 1,
                RefType = "StockTake",
                RefId = order.Id,
                RefNo = order.OrderNo,
                Remark = $"Stocktake variance: book {item.SystemQty}, counted {item.CountedQty}",
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.NickName ?? _currentUser.UserName
            }, item.CountedQty, cancellationToken);

            if (adjust == null)
                continue;
        }

        order.Status = StockTakeStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>取消盘点单（不调整库存）</summary>
    public async Task CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.StockTakeOrders.FindAsync(new object[] { id }, cancellationToken)
                    ?? throw new NotFoundException("Stocktake order not found");

        if (order.Status == StockTakeStatus.Completed)
            throw new BusinessException("Completed stocktake orders cannot be cancelled", ErrorCodes.InvalidStatus);

        order.Status = StockTakeStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除盘点单（仅待盘点状态）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.StockTakeOrders.FindAsync(new object[] { id }, cancellationToken)
                    ?? throw new NotFoundException("Stocktake order not found");

        if (order.Status != StockTakeStatus.Draft)
            throw new BusinessException("Only pending stocktake orders can be deleted", ErrorCodes.InvalidStatus);

        _db.StockTakeItems.RemoveRange(_db.StockTakeItems.Where(x => x.OrderId == id));
        _db.StockTakeOrders.Remove(order);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
