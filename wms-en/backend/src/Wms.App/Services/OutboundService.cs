using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Utils;
using Wms.Im;
using Wms.Im.Entities;

namespace Wms.App.Services;

/// <summary>出库作业服务：单据维护、库存锁定与发运执行</summary>
public class OutboundService
{
    private readonly WmsDbContext _db;
    private readonly InventoryManager _inventory;
    private readonly LocationService _locationService;
    private readonly ICurrentUser _currentUser;

    public OutboundService(
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

    /// <summary>分页查询出库单</summary>
    public async Task<PagedResult<OutboundOrderDto>> QueryAsync(OutboundQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.OutboundOrders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.OrderNo.Contains(keyword) || (x.Remark != null && x.Remark.Contains(keyword)));
        }
        if (query.Status.HasValue) source = source.Where(x => x.Status == query.Status);
        if (query.WarehouseId.HasValue) source = source.Where(x => x.WarehouseId == query.WarehouseId);
        if (query.CustomerId.HasValue) source = source.Where(x => x.CustomerId == query.CustomerId);

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
        var items = await _db.OutboundOrderItems
            .Where(x => orderIds.Contains(x.OrderId))
            .ToListAsync(cancellationToken);

        var customerIds = orders.Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId!.Value).Distinct().ToList();
        var customers = await _db.Customers
            .Where(x => customerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var warehouseIds = orders.Select(x => x.WarehouseId).Distinct().ToList();
        var warehouses = await _db.Warehouses
            .Where(x => warehouseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var result = orders.Select(order =>
        {
            var lines = items.Where(x => x.OrderId == order.Id).ToList();
            return new OutboundOrderDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                Type = order.Type,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerId.HasValue ? customers.GetValueOrDefault(order.CustomerId.Value) : null,
                WarehouseId = order.WarehouseId,
                WarehouseName = warehouses.GetValueOrDefault(order.WarehouseId),
                Status = order.Status,
                PlannedDate = order.PlannedDate,
                Remark = order.Remark,
                CreatedByName = order.CreatedByName,
                CreatedAt = order.CreatedAt,
                ItemCount = lines.Count,
                TotalQty = lines.Sum(x => x.RequestedQty)
            };
        }).ToList();

        return PagedResult<OutboundOrderDto>.Create(result, total, query.Page, query.PageSize);
    }

    /// <summary>获取出库单详情</summary>
    public async Task<OutboundOrderDto> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.OutboundOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Outbound order not found");

        var warehouse = await _db.Warehouses.FindAsync(new object[] { order.WarehouseId }, cancellationToken);
        var customer = order.CustomerId.HasValue
            ? await _db.Customers.FindAsync(new object[] { order.CustomerId.Value }, cancellationToken)
            : null;

        var dto = new OutboundOrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            Type = order.Type,
            CustomerId = order.CustomerId,
            CustomerName = customer?.Name,
            WarehouseId = order.WarehouseId,
            WarehouseName = warehouse?.Name,
            Status = order.Status,
            PlannedDate = order.PlannedDate,
            Remark = order.Remark,
            CreatedByName = order.CreatedByName,
            CreatedAt = order.CreatedAt,
            ItemCount = order.Items.Count,
            TotalQty = order.Items.Sum(x => x.RequestedQty),
            Items = await MapItemsAsync(order.Items, cancellationToken)
        };

        return dto;
    }

    /// <summary>创建出库单（草稿）</summary>
    public async Task<long> CreateAsync(SaveOutboundRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var order = new OutboundOrder
        {
            OrderNo = OrderNoGenerator.Generate("OUT"),
            Type = request.Type,
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            Status = OrderStatus.Draft,
            PlannedDate = request.PlannedDate?.Date ?? DateTime.Today,
            Remark = request.Remark,
            CreatedBy = _currentUser.UserId,
            CreatedByName = _currentUser.NickName ?? _currentUser.UserName
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new OutboundOrderItem
            {
                MaterialId = item.MaterialId,
                LocationId = item.LocationId,
                BatchNo = item.BatchNo?.Trim(),
                RequestedQty = item.RequestedQty,
                ShippedQty = 0,
                Price = item.Price,
                Remark = item.Remark
            });
        }

        _db.OutboundOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }

    /// <summary>更新出库单（仅草稿状态）</summary>
    public async Task UpdateAsync(long id, SaveOutboundRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _db.OutboundOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Outbound order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft outbound orders can be edited", ErrorCodes.InvalidStatus);

        await ValidateAsync(request, cancellationToken);

        order.Type = request.Type;
        order.CustomerId = request.CustomerId;
        order.WarehouseId = request.WarehouseId;
        order.PlannedDate = request.PlannedDate?.Date ?? order.PlannedDate;
        order.Remark = request.Remark;
        order.UpdatedBy = _currentUser.UserId;

        order.Items.Clear();
        foreach (var item in request.Items)
        {
            order.Items.Add(new OutboundOrderItem
            {
                MaterialId = item.MaterialId,
                LocationId = item.LocationId,
                BatchNo = item.BatchNo?.Trim(),
                RequestedQty = item.RequestedQty,
                ShippedQty = 0,
                Price = item.Price,
                Remark = item.Remark
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>确认出库单并锁定库存（草稿 → 已确认）</summary>
    public async Task ConfirmAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.OutboundOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Outbound order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft outbound orders can be confirmed", ErrorCodes.InvalidStatus);

        if (order.Items.Count == 0)
            throw new BusinessException("Outbound order has no line items and cannot be confirmed");

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            if (!item.LocationId.HasValue)
                throw new BusinessException("Specify an outbound location for all line items");

            await _inventory.AllocateAsync(new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = order.WarehouseId,
                LocationId = item.LocationId.Value,
                BatchNo = item.BatchNo,
                Quantity = item.RequestedQty,
                RefType = "Outbound",
                RefId = order.Id,
                RefNo = order.OrderNo,
                Remark = item.Remark,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.NickName ?? _currentUser.UserName
            }, cancellationToken);
        }

        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>执行出库发运（已确认 → 已完成）</summary>
    public async Task CompleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.OutboundOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Outbound order not found");

        if (order.Status != OrderStatus.Confirmed)
            throw new BusinessException("Only confirmed outbound orders can be executed", ErrorCodes.InvalidStatus);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            if (!item.LocationId.HasValue)
                throw new BusinessException("Specify an outbound location for all line items");

            await _inventory.ShipAsync(new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = order.WarehouseId,
                LocationId = item.LocationId.Value,
                BatchNo = item.BatchNo,
                Quantity = item.RequestedQty,
                RefType = "Outbound",
                RefId = order.Id,
                RefNo = order.OrderNo,
                Remark = item.Remark,
                OperatorId = _currentUser.UserId,
                OperatorName = _currentUser.NickName ?? _currentUser.UserName
            }, cancellationToken);

            item.ShippedQty = item.RequestedQty;
        }

        order.Status = OrderStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>取消出库单（已确认状态会释放锁定库存）</summary>
    public async Task CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.OutboundOrders.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Outbound order not found");

        if (order.Status == OrderStatus.Completed)
            throw new BusinessException("Completed outbound orders cannot be cancelled", ErrorCodes.InvalidStatus);

        if (order.Status == OrderStatus.Cancelled)
            return;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (order.Status == OrderStatus.Confirmed)
        {
            foreach (var item in order.Items)
            {
                if (!item.LocationId.HasValue) continue;

                await _inventory.ReleaseAsync(new InventoryOperation
                {
                    MaterialId = item.MaterialId,
                    WarehouseId = order.WarehouseId,
                    LocationId = item.LocationId.Value,
                    BatchNo = item.BatchNo,
                    Quantity = item.RequestedQty,
                    RefType = "Outbound",
                    RefId = order.Id,
                    RefNo = order.OrderNo,
                    Remark = "Outbound order cancelled; locked qty released",
                    OperatorId = _currentUser.UserId,
                    OperatorName = _currentUser.NickName ?? _currentUser.UserName
                }, cancellationToken);
            }
        }

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>删除出库单（仅草稿）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var order = await _db.OutboundOrders.FindAsync(new object[] { id }, cancellationToken)
                    ?? throw new NotFoundException("Outbound order not found");

        if (order.Status != OrderStatus.Draft)
            throw new BusinessException("Only draft outbound orders can be deleted", ErrorCodes.InvalidStatus);

        _db.OutboundOrderItems.RemoveRange(_db.OutboundOrderItems.Where(x => x.OrderId == id));
        _db.OutboundOrders.Remove(order);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(SaveOutboundRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            throw new BusinessException("Add at least one outbound line item");

        var warehouseExists = await _db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
            throw new NotFoundException("Outbound warehouse not found");

        if (request.CustomerId.HasValue)
        {
            var customerExists = await _db.Customers.AnyAsync(x => x.Id == request.CustomerId, cancellationToken);
            if (!customerExists) throw new NotFoundException("Customer not found");
        }

        var materialIds = request.Items.Select(x => x.MaterialId).Distinct().ToList();
        var existsCount = await _db.Materials.CountAsync(x => materialIds.Contains(x.Id), cancellationToken);
        if (existsCount != materialIds.Count)
            throw new BusinessException("Some materials do not exist. Refresh and try again");

        foreach (var item in request.Items)
        {
            if (item.RequestedQty <= 0)
                throw new BusinessException("Outbound quantity must be greater than 0");

            if (item.LocationId.HasValue)
                await _locationService.EnsureBelongsToWarehouseAsync(item.LocationId.Value, request.WarehouseId, cancellationToken);
        }
    }

    private async Task<List<OutboundItemDto>> MapItemsAsync(IEnumerable<OutboundOrderItem> items, CancellationToken cancellationToken)
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
            return new OutboundItemDto
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
                RequestedQty = item.RequestedQty,
                ShippedQty = item.ShippedQty,
                Price = item.Price,
                Remark = item.Remark
            };
        }).ToList();
    }
}
