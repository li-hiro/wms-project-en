namespace Wms.Core.Common;

/// <summary>
/// 所有持久化实体的基类，统一提供主键与审计字段。
/// </summary>
public abstract class BaseEntity
{
    /// <summary>主键</summary>
    public long Id { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>创建人 ID</summary>
    public long? CreatedBy { get; set; }

    /// <summary>更新人 ID</summary>
    public long? UpdatedBy { get; set; }

    /// <summary>逻辑删除标记</summary>
    public bool IsDeleted { get; set; }
}
