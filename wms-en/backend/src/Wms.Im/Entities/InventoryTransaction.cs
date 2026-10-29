using Wms.Core.Common;

namespace Wms.Im.Entities;

/// <summary>
/// 库存流水：记录每一次库存数量变动，作为库存账实相符的审计依据。
/// </summary>
public class InventoryTransaction : BaseEntity
{
    /// <summary>流水号</summary>
    public string No { get; set; } = string.Empty;

    /// <summary>物料 ID</summary>
    public long MaterialId { get; set; }

    /// <summary>仓库 ID</summary>
    public long WarehouseId { get; set; }

    /// <summary>库位 ID</summary>
    public long LocationId { get; set; }

    /// <summary>批次号</summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>流水类型</summary>
    public InventoryTransactionType Type { get; set; }

    /// <summary>在库数量变动（正数入库，负数出库）</summary>
    public decimal Quantity { get; set; }

    /// <summary>锁定数量变动（正数锁定，负数释放）</summary>
    public decimal LockedQuantity { get; set; }

    /// <summary>变动后在库数量</summary>
    public decimal BalanceQty { get; set; }

    /// <summary>变动后锁定数量</summary>
    public decimal LockedBalanceQty { get; set; }

    /// <summary>来源单据类型（Inbound/Outbound/Transfer/StockTake）</summary>
    public string RefType { get; set; } = string.Empty;

    /// <summary>来源单据 ID</summary>
    public long? RefId { get; set; }

    /// <summary>来源单据号</summary>
    public string? RefNo { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>发生时间</summary>
    public DateTime OccurredAt { get; set; } = DateTime.Now;

    /// <summary>操作人 ID</summary>
    public long? OperatorId { get; set; }

    /// <summary>操作人名称</summary>
    public string? OperatorName { get; set; }
}
