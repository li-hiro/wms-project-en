using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Entities;

namespace Wms.App.Services;

/// <summary>物料分类服务</summary>
public class CategoryService
{
    private readonly WmsDbContext _db;

    public CategoryService(WmsDbContext db)
    {
        _db = db;
    }

    /// <summary>查询全部分类（平铺结构，附带上级名称）</summary>
    public async Task<List<CategoryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.MaterialCategories
            .OrderBy(x => x.Sort).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var nameMap = categories.ToDictionary(x => x.Id, x => x.Name);
        return categories.Select(x => new CategoryDto
        {
            Id = x.Id,
            Name = x.Name,
            ParentId = x.ParentId,
            ParentName = x.ParentId > 0 && nameMap.TryGetValue(x.ParentId, out var parentName) ? parentName : "—",
            Sort = x.Sort,
            Status = x.Status,
            Remark = x.Remark
        }).ToList();
    }

    /// <summary>创建分类</summary>
    public async Task<long> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException("Category name cannot be empty");

        var category = new MaterialCategory
        {
            Name = request.Name.Trim(),
            ParentId = request.ParentId,
            Sort = request.Sort,
            Status = request.Status,
            Remark = request.Remark
        };

        _db.MaterialCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }

    /// <summary>更新分类</summary>
    public async Task UpdateAsync(long id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _db.MaterialCategories.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("Category not found");

        if (request.ParentId == id)
            throw new BusinessException("The parent category cannot be itself");

        category.Name = request.Name.Trim();
        category.ParentId = request.ParentId;
        category.Sort = request.Sort;
        category.Status = request.Status;
        category.Remark = request.Remark;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除分类（存在下级分类或已关联物料时禁止删除）</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (await _db.MaterialCategories.AnyAsync(x => x.ParentId == id, cancellationToken))
            throw new BusinessException("Delete subcategories first");

        if (await _db.Materials.AnyAsync(x => x.CategoryId == id, cancellationToken))
            throw new BusinessException("Category still has materials and cannot be deleted");

        var category = await _db.MaterialCategories.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("Category not found");

        _db.MaterialCategories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
