using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.Core.Common;
using Wms.Core.Entities;

namespace Wms.App.Services;

/// <summary>物料主数据服务</summary>
public class MaterialService
{
    private readonly WmsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MaterialService(WmsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>分页查询物料</summary>
    public async Task<PagedResult<MaterialDto>> QueryAsync(MaterialQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.Materials.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Spec != null && x.Spec.Contains(keyword)));
        }

        if (query.CategoryId.HasValue)
            source = source.Where(x => x.CategoryId == query.CategoryId);

        if (query.Status.HasValue)
            source = source.Where(x => x.Status == query.Status);

        if (query.BelowSafetyStock == true)
        {
            source = source.Where(m =>
                (_db.InventoryRecords.Where(r => r.MaterialId == m.Id).Sum(r => r.OnHandQty)) < m.SafetyStock);
        }

        var total = await source.LongCountAsync(cancellationToken);
        var materials = await source
            .OrderBy(x => x.Code)
            .Skip(query.GetSkip())
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var categoryIds = materials.Select(x => x.CategoryId).Distinct().ToList();
        var categories = await _db.MaterialCategories
            .Where(x => categoryIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        var materialIds = materials.Select(x => x.Id).ToList();
        var stockItems = await _db.InventoryRecords
            .Where(x => materialIds.Contains(x.MaterialId))
            .GroupBy(x => x.MaterialId)
            .Select(g => new { MaterialId = g.Key, Qty = g.Sum(x => x.OnHandQty) })
            .ToListAsync(cancellationToken);

        var items = materials.Select(material => new MaterialDto
        {
            Id = material.Id,
            Code = material.Code,
            Name = material.Name,
            Spec = material.Spec,
            CategoryId = material.CategoryId,
            CategoryName = categories.FirstOrDefault(x => x.Id == material.CategoryId)?.Name,
            Unit = material.Unit,
            Brand = material.Brand,
            Price = material.Price,
            SafetyStock = material.SafetyStock,
            Status = material.Status,
            Remark = material.Remark,
            OnHandQty = stockItems.FirstOrDefault(x => x.MaterialId == material.Id)?.Qty ?? 0
        }).ToList();

        return PagedResult<MaterialDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>获取物料下拉选项</summary>
    public async Task<List<OptionDto>> OptionsAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var source = _db.Materials.Where(x => x.Status == CommonStatus.Enabled);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            source = source.Where(x => x.Code.Contains(value) || x.Name.Contains(value));
        }

        return await source
            .OrderBy(x => x.Code)
            .Take(200)
            .Select(x => new OptionDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Extra = x.Spec
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>创建物料</summary>
    public async Task<long> CreateAsync(SaveMaterialRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var code = request.Code.Trim();
        if (await _db.Materials.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ConflictException($"Material code {code} already exists");

        var material = new Material { CreatedBy = _currentUser.UserId };
        Apply(material, request);
        _db.Materials.Add(material);
        await _db.SaveChangesAsync(cancellationToken);
        return material.Id;
    }

    /// <summary>更新物料</summary>
    public async Task UpdateAsync(long id, SaveMaterialRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var material = await _db.Materials.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("Material not found");

        var code = request.Code.Trim();
        if (await _db.Materials.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            throw new ConflictException($"Material code {code} already exists");

        Apply(material, request);
        material.UpdatedBy = _currentUser.UserId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除物料（存在库存或已被单据引用时禁止删除）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var material = await _db.Materials.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("Material not found");

        if (await _db.InventoryRecords.AnyAsync(x => x.MaterialId == id, cancellationToken))
            throw new BusinessException("Material has stock records and cannot be deleted. Disable it instead");

        var referenced = await _db.InboundOrderItems.AnyAsync(x => x.MaterialId == id, cancellationToken)
                         || await _db.OutboundOrderItems.AnyAsync(x => x.MaterialId == id, cancellationToken)
                         || await _db.TransferOrderItems.AnyAsync(x => x.MaterialId == id, cancellationToken);
        if (referenced)
            throw new BusinessException("Material is referenced by transaction orders and cannot be deleted. Disable it instead");

        _db.Materials.Remove(material);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(SaveMaterialRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException("Material code and name are required");
        if (request.Price < 0 || request.SafetyStock < 0)
            throw new BusinessException("Unit price and safety stock cannot be negative");
    }

    private static void Apply(Material material, SaveMaterialRequest request)
    {
        material.Code = request.Code.Trim().ToUpperInvariant();
        material.Name = request.Name.Trim();
        material.Spec = request.Spec;
        material.CategoryId = request.CategoryId;
        material.Unit = string.IsNullOrWhiteSpace(request.Unit) ? "pcs" : request.Unit.Trim();
        material.Brand = request.Brand;
        material.Price = request.Price;
        material.SafetyStock = request.SafetyStock;
        material.Status = request.Status;
        material.Remark = request.Remark;
    }
}
