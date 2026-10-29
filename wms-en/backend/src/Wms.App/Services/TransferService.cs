using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Utils;
using Wms.Im;
using Wms.Im.Entities;

namespace Wms.App.Services;

/// <summary>库存调拨服务：跨仓库/跨库位移库</summary>
public class TransferService
{
    private readonly WmsDbContext _db;
    private readonly InventoryManager _inventory;
    private readonly LocationService _locationService;
    private readonly ICurrentUser _currentUser;

    public TransferService(
        WmsDbContext db,
        InventoryManager inventory,
        LocationService locationService,
        ICurrentUser currentUser)
    {
        _db = db;
        _inventory = inventory;
        _locationService = locationService;
        _currentUser = currentUser;
    }

    /// <summary>分页查询调拨单</summary>
    public async Task<PagedResult<TransferOrderDto>> QueryAsync(TransferQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.TransferOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.OrderNo.Contains(keyword));
        }
        if (query.Status.HasValue) source = source.Where(x => x.Status == query.Status);
        if (query.FromWarehouseId.HasValue) source = source.Where(x => x.FromWarehouseId == query.FromWarehouseId);
        if (query.ToWarehouseId.HasValue) source = source.Where(x => x.ToWarehouseId == query.ToWarehouseId);

        var total = await source.LongCountAsync(cancellationToken);
        var orders = await source
            .OrderByDescending(x => x.Id)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(x => x.Id).ToList();
        var items = await _db.TransferOrderItems.Where(x => orderIds.Contains(x.OrderId)).ToListAsync(cancellationToken);

        var warehouseIds = orders.SelectMany(x => new[] { x.FromWarehouseId, x.ToWarehouseId }).Distinct().ToList();
        var warehouses = await _db.Warehouses
            .Where(x => warehouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var result = orders.Select(order =>
        {
            var lines = items.Where(x => x.OrderId == order.Id).ToList();
            return new TransferOrderDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                FromWarehouseId = order.FromWarehouseId,
                FromWarehouseName = warehouses.GetValueOrDefault(order.FromWarehouseId),
                ToWarehouseId = order.ToWarehouseId,
                ToWarehouseName = warehouses.GetValueOrDefault(order.ToWarehouseId),
                Status = order.Status,
                TransferDate = order.TransferDate,
                Remark = order.Remark,
                CreatedByName = order.CreatedByName,
                CreatedAt = order.CreatedAt,
                ItemCount = lines.Count,
                TotalQty = lines.Sum(x => x.Quantity)
            };
        }).ToList();

        return PagedResult<TransferOrderDto>.Create(result, total, query.Page, query.PageSize);
    }

    /// <summary>获取调拨单详情</summary>
    public async Task<TransferOrderDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.TransferOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Transfer order not found");

        var warehouseIds = new[] { order.FromWarehouseId, order.ToWarehouseId };
        var warehouses = await _db.Warehouses
            .Where(x => warehouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var lines = order.Items.ToList();
        var materialIds = lines.Select(x => x.MaterialId).Distinct().ToList();
        var materials = await _db.Materials
            .Where(x => materialIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code, x.Name, x.Unit })
            .ToListAsync(cancellationToken);

        var locationIds = lines.SelectMany(x => new[] { x.FromLocationId, x.ToLocationId }).Distinct().ToList();
        var locations = await _db.WarehouseLocations
            .Where(x => locationIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code })
            .ToListAsync(cancellationToken);

        return new TransferOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            FromWarehouseId = order.FromWarehouseId,
            FromWarehouseName = warehouses.GetValueOrDefault(order.FromWarehouseId),
            ToWarehouseId = order.ToWarehouseId,
            ToWarehouseName = warehouses.GetValueOrDefault(order.ToWarehouseId),
            Status = order.Status,
            TransferDate = order.TransferDate,
            Remark = order.Remark,
            CreatedByName = order.CreatedByName,
            CreatedAt = order.CreatedAt,
            ItemCount = lines.Count,
            TotalQty = lines.Sum(x => x.Quantity),
            Items = lines.Select(item =>
            {
                var material = materials.FirstOrDefault(x => x.Id == item.MaterialId);
                return new TransferItemDto
                {
                    Id = item.Id,
                    MaterialId = item.MaterialId,
                    MaterialCode = material?.Code ?? string.Empty,
                    MaterialName = material?.Name ?? string.Empty,
                    Unit = material?.Unit ?? string.Empty,
                    FromLocationId = item.FromLocationId,
                    FromLocationCode = locations.FirstOrDefault(x => x.Id == item.FromLocationId)?.Code,
                    ToLocationId = item.ToLocationId,
                    ToLocationCode = locations.FirstOrDefault(x => x.Id == item.ToLocationId)?.Code,
                    BatchNo = item.BatchNo,
                    Quantity = item.Quantity,
                    Remark = item.Remark
                };
            }).ToList()
        };
    }

    /// <summary>创建调拨单（草稿）</summary>
    public async Task<long> CreateAsync(SaveTransferRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var order = new TransferOrder
        {
            OrderNo = OrderNoGenerator.Generate("TR"),
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            Status = OrderStatus.Draft,
            TransferDate = request.TransferDate?.Date ?? DateTime.Today,
            Remark = request.Remark,
            CreatedBy = _currentUser.UserId,
            CreatedByName = _currentUser.NickName ?? _currentUser.UserName
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new TransferOrderItem
            {
                MaterialId = item.MaterialId,
                FromLocationId = item.FromLocationId,
                ToLocationId = item.ToLocationId,
                BatchNo = item.BatchNo?.Trim(),
                Quantity = item.Quantity,
                Remark = item.Remark
            });
        }

        _db.TransferOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    /// <summary>更新调拨单（仅草稿状态）</summary>
    public async Task UpdateAsync(long id, SaveTransferRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _db.TransferOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Transfer order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft transfer orders can be edited", ErrorCodes.InvalidStatus);

        await ValidateAsync(request, cancellationToken);

        order.FromWarehouseId = request.FromWarehouseId;
        order.ToWarehouseId = request.ToWarehouseId;
        order.TransferDate = request.TransferDate?.Date ?? order.TransferDate;
        order.Remark = request.Remark;
        order.UpdatedBy = _currentUser.UserId;

        order.Items.Clear();
        foreach (var item in request.Items)
        {
            order.Items.Add(new TransferOrderItem
            {
                MaterialId = item.MaterialId,
                FromLocationId = item.FromLocationId,
                ToLocationId = item.ToLocationId,
                BatchNo = item.BatchNo?.Trim(),
                Quantity = item.Quantity,
                Remark = item.Remark
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>执行调拨（扣减调出库位库存并增加调入库位库存）</summary>
    public async Task CompleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.TransferOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Transfer order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft transfer orders can be executed", ErrorCodes.InvalidStatus);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            var batchNo = item.BatchNo?.Trim() ?? string.Empty;
            var record = await _db.InventoryRecords.FirstOrDefaultAsync(
                x => x.MaterialId == item.MaterialId
                     && x.WarehouseId == order.FromWarehouseId
                     && x.LocationId == item.FromLocationId
                     && x.BatchNo == batchNo,
                cancellationToken);

            var unitCost = record?.UnitCost ?? 0;

            var operation = new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = order.FromWarehouseId,
                LocationId = item.FromLocationId,
                BatchNo = batchNo,
                Quantity = item.Quantity,
                UnitCost = unitCost,
                RefType = "Transfer",
                RefId = order.Id,
                RefNo = order.OrderNo,
                Remark = item.Remark,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.NickName ?? _currentUser.UserName
            };

            await _inventory.TransferOutAsync(operation, cancellationToken);

            var inbound = operation.With(item.Quantity);
            inbound.WarehouseId = order.ToWarehouseId;
            inbound.LocationId = item.ToLocationId;
            await _inventory.IncreaseAsync(inbound, InventoryTransactionType.TransferIn, cancellationToken);
        }

        order.Status = OrderStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>取消调拨单</summary>
    public async Task CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.TransferOrders.FindAsync(new object[] { id }, cancellationToken)
                    ?? throw new NotFoundException("Transfer order not found");

        if (order.Status == OrderStatus.Completed)
            throw new BusinessException("Completed transfer orders cannot be cancelled", ErrorCodes.InvalidStatus);

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除调拨单（仅草稿）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.TransferOrders.FindAsync(new object[] { id }, cancellationToken)
                    ?? throw new NotFoundException("Transfer order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft transfer orders can be deleted", ErrorCodes.InvalidStatus);

        _db.TransferOrderItems.RemoveRange(_db.TransferOrderItems.Where(x => x.OrderId == id));
        _db.TransferOrders.Remove(order);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(SaveTransferRequest request, CancellationToken cancellationToken)
    {
        if (request.FromWarehouseId == request.ToWarehouseId)
            throw new BusinessException("Source and destination warehouses must be different");

        if (request.Items.Count == 0)
            throw new BusinessException("Add at least one transfer line item");

        var warehouseIds = new[] { request.FromWarehouseId, request.ToWarehouseId };
        var warehouseCount = await _db.Warehouses.CountAsync(x => warehouseIds.Contains(x.Id), cancellationToken);
        if (warehouseCount != 2)
            throw new NotFoundException("Transfer warehouse not found");

        var materialIds = request.Items.Select(x => x.MaterialId).Distinct().ToList();
        var existsCount = await _db.Materials.CountAsync(x => materialIds.Contains(x.Id), cancellationToken);
        if (existsCount != materialIds.Count)
            throw new BusinessException("Some materials do not exist. Refresh and try again");

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                throw new BusinessException("Transfer quantity must be greater than 0");

            await _locationService.EnsureBelongsToWarehouseAsync(item.FromLocationId, request.FromWarehouseId, cancellationToken);
            await _locationService.EnsureBelongsToWarehouseAsync(item.ToLocationId, request.ToWarehouseId, cancellationToken);
        }
    }
}
