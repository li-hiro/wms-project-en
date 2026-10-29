using Wms.Core.Common;

namespace Wms.App.Dtos;

// ---------------- 入库单 ----------------

/// <summary>入库单查询条件</summary>
public class InboundQuery : PagedQuery
{
    public OrderStatus? Status { get; set; }
    public long? WarehouseId { get; set; }
    public long? SupplierId { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>入库单列表项 / 详情</summary>
public class InboundOrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public InboundType Type { get; set; }
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime PlannedDate { get; set; }
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQty { get; set; }
    public List<InboundItemDto> Items { get; set; } = new();
}

/// <summary>入库明细</summary>
public class InboundItemDto
{
    public long Id { get; set; }
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string Unit { get; set; } = string.Empty;
    public long? LocationId { get; set; }
    public string? LocationCode { get; set; }
    public string? BatchNo { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal Price { get; set; }
    public string? Remark { get; set; }
}

/// <summary>保存入库单请求</summary>
public class SaveInboundRequest
{
    public long? Id { get; set; }
    public InboundType Type { get; set; } = InboundType.Purchase;
    public long? SupplierId { get; set; }
    public long WarehouseId { get; set; }
    public DateTime? PlannedDate { get; set; }
    public string? Remark { get; set; }
    public List<InboundItemRequest> Items { get; set; } = new();
}

/// <summary>入库明细请求</summary>
public class InboundItemRequest
{
    public long MaterialId { get; set; }
    public long? LocationId { get; set; }
    public string? BatchNo { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal Price { get; set; }
    public string? Remark { get; set; }
}

// ---------------- 出库单 ----------------

/// <summary>出库单查询条件</summary>
public class OutboundQuery : PagedQuery
{
    public OrderStatus? Status { get; set; }
    public long? WarehouseId { get; set; }
    public long? CustomerId { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>出库单列表项 / 详情</summary>
public class OutboundOrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public OutboundType Type { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime PlannedDate { get; set; }
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQty { get; set; }
    public List<OutboundItemDto> Items { get; set; } = new();
}

/// <summary>出库明细</summary>
public class OutboundItemDto
{
    public long Id { get; set; }
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string Unit { get; set; } = string.Empty;
    public long? LocationId { get; set; }
    public string? LocationCode { get; set; }
    public string? BatchNo { get; set; }
    public decimal RequestedQty { get; set; }
    public decimal ShippedQty { get; set; }
    public decimal Price { get; set; }
    public string? Remark { get; set; }
}

/// <summary>保存出库单请求</summary>
public class SaveOutboundRequest
{
    public long? Id { get; set; }
    public OutboundType Type { get; set; } = OutboundType.Sales;
    public long? CustomerId { get; set; }
    public long WarehouseId { get; set; }
    public DateTime? PlannedDate { get; set; }
    public string? Remark { get; set; }
    public List<OutboundItemRequest> Items { get; set; } = new();
}

/// <summary>出库明细请求</summary>
public class OutboundItemRequest
{
    public long MaterialId { get; set; }
    public long? LocationId { get; set; }
    public string? BatchNo { get; set; }
    public decimal RequestedQty { get; set; }
    public decimal Price { get; set; }
    public string? Remark { get; set; }
}

// ---------------- 盘点单 ----------------

/// <summary>盘点单查询条件</summary>
public class StockTakeQuery : PagedQuery
{
    public StockTakeStatus? Status { get; set; }
    public long? WarehouseId { get; set; }
}

/// <summary>盘点单</summary>
public class StockTakeDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public StockTakeStatus Status { get; set; }
    public DateTime TakeDate { get; set; }
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public int DiffCount { get; set; }
    public List<StockTakeItemDto> Items { get; set; } = new();
}

/// <summary>盘点明细</summary>
public class StockTakeItemDto
{
    public long Id { get; set; }
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public long LocationId { get; set; }
    public string? LocationCode { get; set; }
    public string? BatchNo { get; set; }
    public decimal SystemQty { get; set; }
    public decimal CountedQty { get; set; }
    public decimal DiffQty { get; set; }
}

/// <summary>创建盘点单请求（系统自动生成账面快照）</summary>
public class CreateStockTakeRequest
{
    public long WarehouseId { get; set; }
    public DateTime? TakeDate { get; set; }
    public string? Remark { get; set; }
}

/// <summary>提交实盘数量</summary>
public class UpdateStockTakeItemsRequest
{
    public List<StockTakeCountItem> Items { get; set; } = new();
}

/// <summary>单条实盘数量</summary>
public class StockTakeCountItem
{
    public long Id { get; set; }
    public decimal CountedQty { get; set; }
}

// ---------------- 调拨单 ----------------

/// <summary>调拨单查询条件</summary>
public class TransferQuery : PagedQuery
{
    public OrderStatus? Status { get; set; }
    public long? FromWarehouseId { get; set; }
    public long? ToWarehouseId { get; set; }
}

/// <summary>调拨单</summary>
public class TransferOrderDto
{
    public long Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public long FromWarehouseId { get; set; }
    public string? FromWarehouseName { get; set; }
    public long ToWarehouseId { get; set; }
    public string? ToWarehouseName { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime TransferDate { get; set; }
    public string? Remark { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalQty { get; set; }
    public List<TransferItemDto> Items { get; set; } = new();
}

/// <summary>调拨明细</summary>
public class TransferItemDto
{
    public long Id { get; set; }
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public long FromLocationId { get; set; }
    public string? FromLocationCode { get; set; }
    public long ToLocationId { get; set; }
    public string? ToLocationCode { get; set; }
    public string? BatchNo { get; set; }
    public decimal Quantity { get; set; }
    public string? Remark { get; set; }
}

/// <summary>保存调拨单请求</summary>
public class SaveTransferRequest
{
    public long? Id { get; set; }
    public long FromWarehouseId { get; set; }
    public long ToWarehouseId { get; set; }
    public DateTime? TransferDate { get; set; }
    public string? Remark { get; set; }
    public List<TransferItemRequest> Items { get; set; } = new();
}

/// <summary>调拨明细请求</summary>
public class TransferItemRequest
{
    public long MaterialId { get; set; }
    public long FromLocationId { get; set; }
    public long ToLocationId { get; set; }
    public string? BatchNo { get; set; }
    public decimal Quantity { get; set; }
    public string? Remark { get; set; }
}
