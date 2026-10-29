using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.Core.Common;
using Wms.Core.Entities;

namespace Wms.App.Services;

/// <summary>仓库配置服务</summary>
public class WarehouseService
{
    private readonly WmsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WarehouseService(WmsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>分页查询仓库</summary>
    public async Task<PagedResult<WarehouseDto>> QueryAsync(WarehouseQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.Warehouses.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }
        if (query.Status.HasValue)
            source = source.Where(x => x.Status == query.Status);

        var total = await source.LongCountAsync(cancellationToken);
        var warehouses = await source
            .OrderBy(x => x.Code)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var warehouseIds = warehouses.Select(x => x.Id).ToList();
        var counts = await _db.WarehouseLocations
            .Where(x => warehouseIds.Contains(x.WarehouseId))
            .GroupBy(x => x.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var items = warehouses.Select(x => new WarehouseDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Address = x.Address,
            Manager = x.Manager,
            Phone = x.Phone,
            Status = x.Status,
            Remark = x.Remark,
            LocationCount = counts.FirstOrDefault(c => c.WarehouseId == x.Id)?.Count ?? 0
        }).ToList();

        return PagedResult<WarehouseDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>仓库下拉选项</summary>
    public async Task<List<OptionDto>> OptionsAsync(CancellationToken cancellationToken = default)
        => await _db.Warehouses
            .Where(x => x.Status == CommonStatus.Enabled)
            .OrderBy(x => x.Code)
            .Select(x => new OptionDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

    /// <summary>创建仓库</summary>
    public async Task<long> CreateAsync(SaveWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var code = request.Code.Trim();
        if (await _db.Warehouses.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ConflictException($"Warehouse code {code} already exists");

        var warehouse = new Warehouse { CreatedBy = _currentUser.UserId };
        Apply(warehouse, request);
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync(cancellationToken);
        return warehouse.Id;
    }

    /// <summary>更新仓库</summary>
    public async Task UpdateAsync(long id, SaveWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var warehouse = await _db.Warehouses.FindAsync(new object[] { id }, cancellationToken)
                        ?? throw new NotFoundException("Warehouse not found");

        var code = request.Code.Trim();
        if (await _db.Warehouses.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            throw new ConflictException($"Warehouse code {code} already exists");

        Apply(warehouse, request);
        warehouse.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除仓库（存在库位或库存时禁止删除）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses.FindAsync(new object[] { id }, cancellationToken)
                        ?? throw new NotFoundException("Warehouse not found");

        if (await _db.WarehouseLocations.AnyAsync(x => x.WarehouseId == id, cancellationToken))
            throw new BusinessException("Warehouse still has locations and cannot be deleted");

        if (await _db.InventoryRecords.AnyAsync(x => x.WarehouseId == id, cancellationToken))
            throw new BusinessException("Warehouse still has stock records and cannot be deleted");

        _db.Warehouses.Remove(warehouse);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(SaveWarehouseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException("Warehouse code and name are required");
    }

    private static void Apply(Warehouse warehouse, SaveWarehouseRequest request)
    {
        warehouse.Code = request.Code.Trim().ToUpperInvariant();
        warehouse.Name = request.Name.Trim();
        warehouse.Address = request.Address;
        warehouse.Manager = request.Manager;
        warehouse.Phone = request.Phone;
        warehouse.Status = request.Status;
        warehouse.Remark = request.Remark;
    }
}
