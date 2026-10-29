using Wms.Core.Common;

namespace Wms.Core.Entities;

/// <summary>物料分类</summary>
public class MaterialCategory : BaseEntity
{
    /// <summary>分类名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>上级分类 ID，0 表示顶级</summary>
    public long ParentId { get; set; }

    /// <summary>排序值，越小越靠前</summary>
    public int Sort { get; set; }

    /// <summary>状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>物料主数据</summary>
public class Material : BaseEntity
{
    /// <summary>物料编码（全局唯一）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>物料名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>规格型号</summary>
    public string? Spec { get; set; }

    /// <summary>所属分类</summary>
    public long CategoryId { get; set; }

    /// <summary>计量单位</summary>
    public string Unit { get; set; } = "pcs";

    /// <summary>品牌/厂商</summary>
    public string? Brand { get; set; }

    /// <summary>参考单价（元）</summary>
    public decimal Price { get; set; }

    /// <summary>安全库存下限，低于该值时触发预警</summary>
    public decimal SafetyStock { get; set; }

    /// <summary>状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>供应商</summary>
public class Supplier : BaseEntity
{
    /// <summary>供应商编码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>供应商名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>联系人</summary>
    public string? Contact { get; set; }

    /// <summary>联系电话</summary>
    public string? Phone { get; set; }

    /// <summary>地址</summary>
    public string? Address { get; set; }

    /// <summary>状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>客户</summary>
public class Customer : BaseEntity
{
    /// <summary>客户编码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>客户名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>联系人</summary>
    public string? Contact { get; set; }

    /// <summary>联系电话</summary>
    public string? Phone { get; set; }

    /// <summary>地址</summary>
    public string? Address { get; set; }

    /// <summary>状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>仓库</summary>
public class Warehouse : BaseEntity
{
    /// <summary>仓库编码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>仓库名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>地址</summary>
    public string? Address { get; set; }

    /// <summary>仓库负责人</summary>
    public string? Manager { get; set; }

    /// <summary>联系电话</summary>
    public string? Phone { get; set; }

    /// <summary>状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>库位</summary>
public class WarehouseLocation : BaseEntity
{
    /// <summary>所属仓库</summary>
    public long WarehouseId { get; set; }

    /// <summary>库位编码，仓库内唯一</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>库位名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>库区，如 A 区、常温区</summary>
    public string? Zone { get; set; }

    /// <summary>库位类型</summary>
    public LocationType Type { get; set; } = LocationType.Storage;

    /// <summary>设计容量（单位与物料一致，仅作参考）</summary>
    public decimal Capacity { get; set; }

    /// <summary>状态</summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
