using Wms.Core.Common;

namespace Wms.App.Dtos;

// ---------------- 物料分类 ----------------

/// <summary>物料分类</summary>
public class CategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Sort { get; set; }
    public CommonStatus Status { get; set; }
    public string? Remark { get; set; }
}

/// <summary>保存物料分类请求</summary>
public class SaveCategoryRequest
{
    public long? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long ParentId { get; set; }
    public int Sort { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public string? Remark { get; set; }
}

// ---------------- 物料 ----------------

/// <summary>物料查询条件</summary>
public class MaterialQuery : PagedQuery
{
    public long? CategoryId { get; set; }
    public CommonStatus? Status { get; set; }
    public bool? BelowSafetyStock { get; set; }
}

/// <summary>物料列表项</summary>
public class MaterialDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public long CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public decimal Price { get; set; }
    public decimal SafetyStock { get; set; }
    public CommonStatus Status { get; set; }
    public string? Remark { get; set; }
    public decimal OnHandQty { get; set; }
}

/// <summary>保存物料请求</summary>
public class SaveMaterialRequest
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public long CategoryId { get; set; }
    public string Unit { get; set; } = "pcs";
    public string? Brand { get; set; }
    public decimal Price { get; set; }
    public decimal SafetyStock { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public string? Remark { get; set; }
}

// ---------------- 供应商 / 客户 ----------------

/// <summary>往来单位查询条件</summary>
public class PartnerQuery : PagedQuery
{
    public CommonStatus? Status { get; set; }
}

/// <summary>供应商 / 客户</summary>
public class PartnerDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public CommonStatus Status { get; set; }
    public string? Remark { get; set; }
}

/// <summary>保存供应商 / 客户请求</summary>
public class SavePartnerRequest
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Contact { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public string? Remark { get; set; }
}

// ---------------- 仓库 / 库位 ----------------

/// <summary>仓库查询条件</summary>
public class WarehouseQuery : PagedQuery
{
    public CommonStatus? Status { get; set; }
}

/// <summary>仓库信息</summary>
public class WarehouseDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Manager { get; set; }
    public string? Phone { get; set; }
    public CommonStatus Status { get; set; }
    public string? Remark { get; set; }
    public int LocationCount { get; set; }
}

/// <summary>保存仓库请求</summary>
public class SaveWarehouseRequest
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Manager { get; set; }
    public string? Phone { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>库位查询条件</summary>
public class LocationQuery : PagedQuery
{
    public long? WarehouseId { get; set; }
    public CommonStatus? Status { get; set; }
}

/// <summary>库位信息</summary>
public class LocationDto
{
    public long Id { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public LocationType Type { get; set; }
    public decimal Capacity { get; set; }
    public CommonStatus Status { get; set; }
    public string? Remark { get; set; }
    public decimal OnHandQty { get; set; }
}

/// <summary>保存库位请求</summary>
public class SaveLocationRequest
{
    public long? Id { get; set; }
    public long WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Zone { get; set; }
    public LocationType Type { get; set; } = LocationType.Storage;
    public decimal Capacity { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    public string? Remark { get; set; }
}
