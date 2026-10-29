using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Utils;
using Wms.Im;
using Wms.Im.Entities;

namespace Wms.App.Services;

/// <summary>入库作业服务：单据维护、确认与库存入库执行</summary>
public class InboundService
{
    private readonly WmsDbContext _db;
    private readonly InventoryManager _inventory;
    private readonly LocationService _locationService;
    private readonly ICurrentUser _currentUser;

    public InboundService(
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

    /// <summary>分页查询入库单</summary>
    public async Task<PagedResult<InboundOrderDto>> QueryAsync(InboundQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.InboundOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.OrderNo.Contains(keyword) || (x.Remark != null && x.Remark.Contains(keyword)));
        }
        if (query.Status.HasValue) source = source.Where(x => x.Status == query.Status);
        if (query.WarehouseId.HasValue) source = source.Where(x => x.WarehouseId == query.WarehouseId);
        if (query.SupplierId.HasValue) source = source.Where(x => x.SupplierId == query.SupplierId);

        var start = DateRange.Parse(query.StartDate);
        if (start.HasValue) source = source.Where(x => x.CreatedAt >= start.Value);
        var end = DateRange.Parse(query.EndDate);
        if (end.HasValue) source = source.Where(x => x.CreatedAt < DateRange.EndOfDayExclusive(end.Value));

        var total = await source.LongCountAsync(cancellationToken);
        var orders = await source
            .OrderByDescending(x => x.Id)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(x => x.Id).ToList();
        var items = await _db.InboundOrderItems
            .Where(x => orderIds.Contains(x.OrderId))
            .ToListAsync(cancellationToken);

        var suppliers = await LoadSupplierNamesAsync(orders.Select(x => x.SupplierId).Where(x => x.HasValue).Select(x => x!.Value), cancellationToken);
        var warehouses = await LoadWarehouseNamesAsync(orders.Select(x => x.WarehouseId), cancellationToken);

        var result = orders.Select(order =>
        {
            var lines = items.Where(x => x.OrderId == order.Id).ToList();
            return new InboundOrderDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                Type = order.Type,
                SupplierId = order.SupplierId,
                SupplierName = order.SupplierId.HasValue ? suppliers.GetValueOrDefault(order.SupplierId.Value) : null,
                WarehouseId = order.WarehouseId,
                WarehouseName = warehouses.GetValueOrDefault(order.WarehouseId),
                Status = order.Status,
                PlannedDate = order.PlannedDate,
                Remark = order.Remark,
                CreatedByName = order.CreatedByName,
                CreatedAt = order.CreatedAt,
                ItemCount = lines.Count,
                TotalQty = lines.Sum(x => x.PlannedQty)
            };
        }).ToList();

        return PagedResult<InboundOrderDto>.Create(result, total, query.Page, query.PageSize);
    }

    /// <summary>获取入库单详情</summary>
    public async Task<InboundOrderDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.InboundOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Inbound order not found");

        var dto = new InboundOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            Type = order.Type,
            SupplierId = order.SupplierId,
            WarehouseId = order.WarehouseId,
            Status = order.Status,
            PlannedDate = order.PlannedDate,
            Remark = order.Remark,
            CreatedByName = order.CreatedByName,
            CreatedAt = order.CreatedAt,
            ItemCount = order.Items.Count,
            TotalQty = order.Items.Sum(x => x.PlannedQty)
        };

        var suppliers = await LoadSupplierNamesAsync(order.SupplierId.HasValue ? new[] { order.SupplierId.Value } : Array.Empty<long>(), cancellationToken);
        var warehouses = await LoadWarehouseNamesAsync(new[] { order.WarehouseId }, cancellationToken);
        dto.SupplierName = order.SupplierId.HasValue ? suppliers.GetValueOrDefault(order.SupplierId.Value) : null;
        dto.WarehouseName = warehouses.GetValueOrDefault(order.WarehouseId);

        dto.Items = await MapItemsAsync(order.Items, cancellationToken);
        return dto;
    }

    /// <summary>创建入库单（草稿）</summary>
    public async Task<long> CreateAsync(SaveInboundRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var order = new InboundOrder
        {
            OrderNo = OrderNoGenerator.Generate("IN"),
            Type = request.Type,
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            Status = OrderStatus.Draft,
            PlannedDate = request.PlannedDate?.Date ?? DateTime.Today,
            Remark = request.Remark,
            CreatedBy = _currentUser.UserId,
            CreatedByName = _currentUser.NickName ?? _currentUser.UserName
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new InboundOrderItem
            {
                MaterialId = item.MaterialId,
                LocationId = item.LocationId,
                BatchNo = item.BatchNo?.Trim(),
                PlannedQty = item.PlannedQty,
                ReceivedQty = item.PlannedQty,
                Price = item.Price,
                Remark = item.Remark
            });
        }

        _db.InboundOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    /// <summary>更新入库单（仅草稿状态）</summary>
    public async Task UpdateAsync(long id, SaveInboundRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _db.InboundOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Inbound order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft inbound orders can be edited", ErrorCodes.InvalidStatus);

        await ValidateAsync(request, cancellationToken);

        order.Type = request.Type;
        order.SupplierId = request.SupplierId;
        order.WarehouseId = request.WarehouseId;
        order.PlannedDate = request.PlannedDate?.Date ?? order.PlannedDate;
        order.Remark = request.Remark;
        order.UpdatedBy = _currentUser.UserId;

        order.Items.Clear();
        foreach (var item in request.Items)
        {
            order.Items.Add(new InboundOrderItem
            {
                MaterialId = item.MaterialId,
                LocationId = item.LocationId,
                BatchNo = item.BatchNo?.Trim(),
                PlannedQty = item.PlannedQty,
                ReceivedQty = item.PlannedQty,
                Price = item.Price,
                Remark = item.Remark
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>确认入库单（草稿 → 已确认）</summary>
    public async Task ConfirmAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.InboundOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Inbound order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft inbound orders can be confirmed", ErrorCodes.InvalidStatus);

        if (order.Items.Count == 0)
            throw new BusinessException("Inbound order has no line items and cannot be confirmed");

        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>执行入库（已确认 → 已完成），同时增加库存并生成流水</summary>
    public async Task CompleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.InboundOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Inbound order not found");

        if (order.Status != OrderStatus.Confirmed)
            throw new BusinessException("Only confirmed inbound orders can be executed", ErrorCodes.InvalidStatus);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            if (!item.LocationId.HasValue)
                throw new BusinessException("Specify an inbound location for all line items");

            var quantity = item.ReceivedQty > 0 ? item.ReceivedQty : item.PlannedQty;
            if (quantity <= 0)
                throw new BusinessException("Inbound quantity must be greater than 0");

            await _inventory.IncreaseAsync(new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = order.WarehouseId,
                LocationId = item.LocationId.Value,
                BatchNo = item.BatchNo,
                Quantity = quantity,
                UnitCost = item.Price,
                RefType = "Inbound",
                RefId = order.Id,
                RefNo = order.OrderNo,
                Remark = item.Remark,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.NickName ?? _currentUser.UserName
            }, InventoryTransactionType.Inbound, cancellationToken);

            item.ReceivedQty = quantity;
        }

        order.Status = OrderStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>取消入库单</summary>
    public async Task CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.InboundOrders.FindAsync(new object[] { id }, cancellationToken)
                    ?? throw new NotFoundException("Inbound order not found");

        if (order.Status == OrderStatus.Completed)
            throw new BusinessException("Completed inbound orders cannot be cancelled", ErrorCodes.InvalidStatus);

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除入库单（仅草稿）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.InboundOrders.FindAsync(new object[] { id }, cancellationToken)
                    ?? throw new NotFoundException("Inbound order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft inbound orders can be deleted", ErrorCodes.InvalidStatus);

        _db.InboundOrderItems.RemoveRange(_db.InboundOrderItems.Where(x => x.OrderId == id));
        _db.InboundOrders.Remove(order);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(SaveInboundRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            throw new BusinessException("Add at least one inbound line item");

        var warehouseExists = await _db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException("Inbound warehouse not found");

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await _db.Suppliers.AnyAsync(x => x.Id == request.SupplierId, cancellationToken);
            if (!supplierExists) throw new NotFoundException("Supplier not found");
        }

        var materialIds = request.Items.Select(x => x.MaterialId).Distinct().ToList();
        var existsCount = await _db.Materials.CountAsync(x => materialIds.Contains(x.Id), cancellationToken);
        if (existsCount != materialIds.Count)
            throw new BusinessException("Some materials do not exist. Refresh and try again");

        foreach (var item in request.Items)
        {
            if (item.PlannedQty <= 0)
                throw new BusinessException("Inbound quantity must be greater than 0");

            if (item.LocationId.HasValue)
                await _locationService.EnsureBelongsToWarehouseAsync(item.LocationId.Value, request.WarehouseId, cancellationToken);
        }
    }

    private async Task<List<InboundItemDto>> MapItemsAsync(IEnumerable<InboundOrderItem> items, CancellationToken cancellationToken)
    {
        var lines = items.ToList();
        var materialIds = lines.Select(x => x.MaterialId).Distinct().ToList();
        var locationIds = lines.Where(x => x.LocationId.HasValue).Select(x => x.LocationId!.Value).Distinct().ToList();

        var materials = await _db.Materials
            .Where(x => materialIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code, x.Name, x.Spec, x.Unit })
            .ToListAsync(cancellationToken);

        var locations = await _db.WarehouseLocations
            .Where(x => locationIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Code })
            .ToListAsync(cancellationToken);

        return lines.Select(item =>
        {
            var material = materials.FirstOrDefault(x => x.Id == item.MaterialId);
            return new InboundItemDto
            {
                Id = item.Id,
                MaterialId = item.MaterialId,
                MaterialCode = material?.Code ?? string.Empty,
                MaterialName = material?.Name ?? string.Empty,
                Spec = material?.Spec,
                Unit = material?.Unit ?? string.Empty,
                LocationId = item.LocationId,
                LocationCode = item.LocationId.HasValue
                    ? locations.FirstOrDefault(x => x.Id == item.LocationId.Value)?.Code
                    : null,
                BatchNo = item.BatchNo,
                PlannedQty = item.PlannedQty,
                ReceivedQty = item.ReceivedQty,
                Price = item.Price,
                Remark = item.Remark
            };
        }).ToList();
    }

    private async Task<Dictionary<long, string>> LoadSupplierNamesAsync(IEnumerable<long> supplierIds, CancellationToken cancellationToken)
    {
        var ids = supplierIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, string>();

        return await _db.Suppliers
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
    }

    private async Task<Dictionary<long, string>> LoadWarehouseNamesAsync(IEnumerable<long> warehouseIds, CancellationToken cancellationToken)
    {
        var ids = warehouseIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, string>();

        return await _db.Warehouses
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
    }
}
