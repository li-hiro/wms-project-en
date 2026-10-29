using Wms.Im.Entities;

namespace Wms.Im;

/// <summary>
/// 库存操作指令：描述一次库存变动所需的业务上下文。
/// </summary>
public class InventoryOperation
{
    public long MaterialId { get; set; }
    public long WarehouseId { get; set; }
    public long LocationId { get; set; }
    public string? BatchNo { get; set; }
    public decimal Quantity { get; set; }
    public string RefType { get; set; } = string.Empty;
    public long? RefId { get; set; }
    public string? RefNo { get; set; }
    public string? Remark { get; set; }
    public decimal UnitCost { get; set; }
    public long? OperatorId { get; set; }
    public string? OperatorName { get; set; }

    /// <summary>规范化批次号，未启用批次管理时统一为空字符串</summary>
    public string NormalizedBatchNo => string.IsNullOrWhiteSpace(BatchNo) ? string.Empty : BatchNo.Trim();

    /// <summary>创建操作指令的深拷贝，便于按物料行批量执行</summary>
    public InventoryOperation With(decimal quantity) => new()
    {
        MaterialId = MaterialId,
        WarehouseId = WarehouseId,
        LocationId = LocationId,
        BatchNo = BatchNo,
        Quantity = quantity,
        RefType = RefType,
        RefId = RefId,
        RefNo = RefNo,
        Remark = Remark,
        UnitCost = UnitCost,
        OperatorId = OperatorId,
        OperatorName = OperatorName
    };
}
