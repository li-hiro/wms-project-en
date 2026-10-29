using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.Core.Common;
using Wms.Core.Entities;

namespace Wms.App.Services;

/// <summary>库位服务</summary>
public class LocationService
{
    private readonly WmsDbContext _db;

    public LocationService(WmsDbContext db)
    {
        _db = db;
    }

    /// <summary>分页查询库位</summary>
    public async Task<PagedResult<LocationDto>> QueryAsync(LocationQuery query, CancellationToken cancellationToken = default)
    {
        var source = from location in _db.WarehouseLocations
                     join warehouse in _db.Warehouses on location.WarehouseId equals warehouse.Id
                     select new { location, warehouse };

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.location.Code.Contains(keyword)
                                       || x.location.Name.Contains(keyword)
                                       || (x.location.Zone != null && x.location.Zone.Contains(keyword)));
        }
        if (query.WarehouseId.HasValue)
            source = source.Where(x => x.location.WarehouseId == query.WarehouseId);
        if (query.Status.HasValue)
            source = source.Where(x => x.location.Status == query.Status);

        var total = await source.LongCountAsync(cancellationToken);
        var rows = await source
            .OrderBy(x => x.warehouse.Code).ThenBy(x => x.location.Code)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var locationIds = rows.Select(x => x.location.Id).ToList();
        var stockItems = await _db.InventoryRecords
            .Where(x => locationIds.Contains(x.LocationId))
            .GroupBy(x => x.LocationId)
            .Select(g => new { LocationId = g.Key, Qty = g.Sum(x => x.OnHandQty) })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => new LocationDto
        {
            Id = row.location.Id,
            WarehouseId = row.location.WarehouseId,
            WarehouseName = row.warehouse.Name,
            Code = row.location.Code,
            Name = row.location.Name,
            Zone = row.location.Zone,
            Type = row.location.Type,
            Capacity = row.location.Capacity,
            Status = row.location.Status,
            Remark = row.location.Remark,
            OnHandQty = stockItems.FirstOrDefault(x => x.LocationId == row.location.Id)?.Qty ?? 0
        }).ToList();

        return PagedResult<LocationDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>按仓库查询可用库位（下拉选项）</summary>
    public async Task<List<OptionDto>> OptionsAsync(long? warehouseId, CancellationToken cancellationToken = default)
    {
        var source = _db.WarehouseLocations.Where(x => x.Status == CommonStatus.Enabled);
        if (warehouseId.HasValue)
            source = source.Where(x => x.WarehouseId == warehouseId);

        return await source
            .OrderBy(x => x.Code)
            .Take(500)
            .Select(x => new OptionDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Extra = x.Zone
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>创建库位</summary>
    public async Task<long> CreateAsync(SaveLocationRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        await EnsureWarehouseAsync(request.WarehouseId, cancellationToken);

        var code = request.Code.Trim();
        if (await _db.WarehouseLocations.AnyAsync(x => x.WarehouseId == request.WarehouseId && x.Code == code, cancellationToken))
            throw new ConflictException($"Location code {code} already exists in this warehouse");

        var location = new WarehouseLocation
        {
            WarehouseId = request.WarehouseId,
            Code = code,
            Name = string.IsNullOrWhiteSpace(request.Name) ? code : request.Name.Trim(),
            Zone = request.Zone,
            Type = request.Type,
            Capacity = request.Capacity,
            Status = request.Status,
            Remark = request.Remark
        };

        _db.WarehouseLocations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);
        return location.Id;
    }

    /// <summary>更新库位</summary>
    public async Task UpdateAsync(long id, SaveLocationRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        await EnsureWarehouseAsync(request.WarehouseId, cancellationToken);

        var location = await _db.WarehouseLocations.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("Location not found");

        var code = request.Code.Trim();
        if (await _db.WarehouseLocations.AnyAsync(
                x => x.WarehouseId == request.WarehouseId && x.Code == code && x.Id != id, cancellationToken))
            throw new ConflictException($"Location code {code} already exists in this warehouse");

        location.WarehouseId = request.WarehouseId;
        location.Code = code;
        location.Name = string.IsNullOrWhiteSpace(request.Name) ? code : request.Name.Trim();
        location.Zone = request.Zone;
        location.Type = request.Type;
        location.Capacity = request.Capacity;
        location.Status = request.Status;
        location.Remark = request.Remark;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除库位（存在库存时禁止删除）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var location = await _db.WarehouseLocations.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("Location not found");

        if (await _db.InventoryRecords.AnyAsync(x => x.LocationId == id && x.OnHandQty > 0, cancellationToken))
            throw new BusinessException("Location still has stock and cannot be deleted");

        _db.WarehouseLocations.Remove(location);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>校验库位归属仓库，供入库/出库/调拨服务复用</summary>
    public async Task EnsureBelongsToWarehouseAsync(long locationId, long warehouseId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.WarehouseLocations.AnyAsync(
            x => x.Id == locationId && x.WarehouseId == warehouseId && x.Status == CommonStatus.Enabled,
            cancellationToken);

        if (!exists)
            throw new BusinessException("The selected location does not belong to this warehouse or is disabled");
    }

    private async Task EnsureWarehouseAsync(long warehouseId, CancellationToken cancellationToken)
    {
        var exists = await _db.Warehouses.AnyAsync(x => x.Id == warehouseId, cancellationToken);
        if (!exists)
            throw new NotFoundException("Warehouse not found");
    }

    private static void Validate(SaveLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new BusinessException("Location code cannot be empty");
        if (request.Capacity < 0)
            throw new BusinessException("Location capacity cannot be negative");
    }
}
