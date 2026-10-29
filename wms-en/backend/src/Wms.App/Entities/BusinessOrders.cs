using System.ComponentModel.DataAnnotations.Schema;
using Wms.Core.Common;

namespace Wms.App.Entities;

/// <summary>入库单</summary>
public class InboundOrder : BaseEntity
{
    /// <summary>入库单号</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>入库类型</summary>
    public InboundType Type { get; set; } = InboundType.Purchase;

    /// <summary>供应商 ID</summary>
    public long? SupplierId { get; set; }

    /// <summary>入库仓库 ID</summary>
    public long WarehouseId { get; set; }

    /// <summary>单据状态</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    /// <summary>计划入库日期</summary>
    public DateTime PlannedDate { get; set; } = DateTime.Today;

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>创建人名称（冗余存储，便于列表展示）</summary>
    public string? CreatedByName { get; set; }

    /// <summary>入库明细</summary>
    public List<InboundOrderItem> Items { get; set; } = new();

    /// <summary>明细总数量（不持久化）</summary>
    [NotMapped]
    public decimal TotalQty => Items.Sum(x => x.PlannedQty);
}

/// <summary>入库单明细</summary>
public class InboundOrderItem : BaseEntity
{
    public long OrderId { get; set; }
    public long MaterialId { get; set; }

    /// <summary>入库库位</summary>
    public long? LocationId { get; set; }

    /// <summary>批次号</summary>
    public string? BatchNo { get; set; }

    /// <summary>计划数量</summary>
    public decimal PlannedQty { get; set; }

    /// <summary>实收数量</summary>
    public decimal ReceivedQty { get; set; }

    /// <summary>入库单价</summary>
    public decimal Price { get; set; }

    public string? Remark { get; set; }
}

/// <summary>出库单</summary>
public class OutboundOrder : BaseEntity
{
    /// <summary>出库单号</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>出库类型</summary>
    public OutboundType Type { get; set; } = OutboundType.Sales;

    /// <summary>客户 ID</summary>
    public long? CustomerId { get; set; }

    /// <summary>出库仓库 ID</summary>
    public long WarehouseId { get; set; }

    /// <summary>单据状态</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    /// <summary>计划出库日期</summary>
    public DateTime PlannedDate { get; set; } = DateTime.Today;

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>创建人名称</summary>
    public string? CreatedByName { get; set; }

    /// <summary>出库明细</summary>
    public List<OutboundOrderItem> Items { get; set; } = new();

    /// <summary>明细总数量（不持久化）</summary>
    [NotMapped]
    public decimal TotalQty => Items.Sum(x => x.RequestedQty);
}

/// <summary>出库单明细</summary>
public class OutboundOrderItem : BaseEntity
{
    public long OrderId { get; set; }
    public long MaterialId { get; set; }

    /// <summary>出库库位</summary>
    public long? LocationId { get; set; }

    /// <summary>批次号</summary>
    public string? BatchNo { get; set; }

    /// <summary>申请数量</summary>
    public decimal RequestedQty { get; set; }

    /// <summary>实发数量</summary>
    public decimal ShippedQty { get; set; }

    /// <summary>出库单价</summary>
    public decimal Price { get; set; }

    public string? Remark { get; set; }
}

/// <summary>盘点单</summary>
public class StockTakeOrder : BaseEntity
{
    /// <summary>盘点单号</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>盘点仓库</summary>
    public long WarehouseId { get; set; }

    /// <summary>盘点状态</summary>
    public StockTakeStatus Status { get; set; } = StockTakeStatus.Draft;

    /// <summary>盘点日期</summary>
    public DateTime TakeDate { get; set; } = DateTime.Today;

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>创建人名称</summary>
    public string? CreatedByName { get; set; }

    /// <summary>盘点明细</summary>
    public List<StockTakeItem> Items { get; set; } = new();
}

/// <summary>盘点明细</summary>
public class StockTakeItem : BaseEntity
{
    public long OrderId { get; set; }
    public long MaterialId { get; set; }

    /// <summary>库存台账 ID</summary>
    public long InventoryRecordId { get; set; }

    public long LocationId { get; set; }
    public string? BatchNo { get; set; }

    /// <summary>账面数量（生成盘点单时的快照）</summary>
    public decimal SystemQty { get; set; }

    /// <summary>实盘数量</summary>
    public decimal CountedQty { get; set; }

    /// <summary>差异数量（实盘 - 账面，不持久化）</summary>
    [NotMapped]
    public decimal DiffQty => CountedQty - SystemQty;
}

/// <summary>调拨单</summary>
public class TransferOrder : BaseEntity
{
    /// <summary>调拨单号</summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>调出仓库</summary>
    public long FromWarehouseId { get; set; }

    /// <summary>调入仓库</summary>
    public long ToWarehouseId { get; set; }

    /// <summary>单据状态</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    /// <summary>计划调拨日期</summary>
    public DateTime TransferDate { get; set; } = DateTime.Today;

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>创建人名称</summary>
    public string? CreatedByName { get; set; }

    /// <summary>调拨明细</summary>
    public List<TransferOrderItem> Items { get; set; } = new();
}

/// <summary>调拨明细</summary>
public class TransferOrderItem : BaseEntity
{
    public long OrderId { get; set; }
    public long MaterialId { get; set; }

    /// <summary>调出库位</summary>
    public long FromLocationId { get; set; }

    /// <summary>调入库位</summary>
    public long ToLocationId { get; set; }

    /// <summary>批次号</summary>
    public string? BatchNo { get; set; }

    /// <summary>调拨数量</summary>
    public decimal Quantity { get; set; }

    public string? Remark { get; set; }
}
