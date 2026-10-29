using Wms.Core.Common;

namespace Wms.App.Dtos;

/// <summary>库存查询条件</summary>
public class InventoryQuery : PagedQuery
{
    public long? WarehouseId { get; set; }
    public long? LocationId { get; set; }
    public long? MaterialId { get; set; }
    public bool? OnlyInStock { get; set; }
    public bool? OnlyAlerts { get; set; }
}

/// <summary>库存台账列表项</summary>
public class InventoryItemDto
{
    public long Id { get; set; }
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string Unit { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public long LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public decimal OnHandQty { get; set; }
    public decimal LockedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Amount { get; set; }
    public decimal SafetyStock { get; set; }
    public bool Alert { get; set; }
    public DateTime? LastInAt { get; set; }
    public DateTime? LastOutAt { get; set; }
}

/// <summary>库存流水查询条件</summary>
public class TransactionQuery : PagedQuery
{
    public InventoryTransactionType? Type { get; set; }
    public long? MaterialId { get; set; }
    public long? WarehouseId { get; set; }
    public string? RefNo { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
}

/// <summary>库存流水列表项</summary>
public class TransactionDto
{
    public long Id { get; set; }
    public string No { get; set; } = string.Empty;
    public InventoryTransactionType Type { get; set; }
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string BatchNo { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LockedQuantity { get; set; }
    public decimal BalanceQty { get; set; }
    public string RefType { get; set; } = string.Empty;
    public string? RefNo { get; set; }
    public string? Remark { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? OperatorName { get; set; }
}

/// <summary>按仓库汇总的库存概览</summary>
public class InventorySummaryDto
{
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int SkuCount { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal LockedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>库存预警项（按物料 + 仓库汇总后低于安全库存）</summary>
public class AlertItemDto
{
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string? Spec { get; set; }
    public string Unit { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal OnHandQty { get; set; }
    public decimal LockedQty { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ShortageQty { get; set; }
}
