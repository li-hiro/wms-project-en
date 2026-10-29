using System.ComponentModel.DataAnnotations.Schema;
using Wms.Core.Common;

namespace Wms.Im.Entities;

/// <summary>
/// 库存台账（聚合根）：以「物料 + 仓库 + 库位 + 批次」为最小维度记录库存数量。
/// 所有库存数量变更必须通过聚合方法完成，以保证可用库存与锁定库存的一致性。
/// </summary>
public class InventoryRecord : BaseEntity
{
    /// <summary>物料 ID</summary>
    public long MaterialId { get; set; }

    /// <summary>仓库 ID</summary>
    public long WarehouseId { get; set; }

    /// <summary>库位 ID</summary>
    public long LocationId { get; set; }

    /// <summary>批次号，无批次管理时为空字符串</summary>
    public string BatchNo { get; set; } = string.Empty;

    /// <summary>在库数量（实物数量）</summary>
    public decimal OnHandQty { get; set; }

    /// <summary>锁定数量（被出库单占用，尚未实际出库）</summary>
    public decimal LockedQty { get; set; }

    /// <summary>移动加权平均成本单价</summary>
    public decimal UnitCost { get; set; }

    /// <summary>最后入库时间</summary>
    public DateTime? LastInAt { get; set; }

    /// <summary>最后出库时间</summary>
    public DateTime? LastOutAt { get; set; }

    /// <summary>可用数量 = 在库数量 - 锁定数量（不持久化）</summary>
    [NotMapped]
    public decimal AvailableQty => OnHandQty - LockedQty;

    /// <summary>入库：增加在库数量，并按移动加权平均法更新成本</summary>
    /// <param name="quantity">入库数量</param>
    /// <param name="unitCost">本次入库单价，为 0 时保持原成本</param>
    public void Increase(decimal quantity, decimal unitCost = 0)
    {
        if (quantity <= 0) throw new BusinessException("Inbound quantity must be greater than 0");

        if (unitCost > 0 && quantity > 0)
        {
            var totalQty = OnHandQty + quantity;
            UnitCost = totalQty == 0
                ? unitCost
                : Math.Round((OnHandQty * UnitCost + quantity * unitCost) / totalQty, 4);
        }

        OnHandQty += quantity;
        LastInAt = DateTime.Now;
    }

    /// <summary>锁定库存：在库数量不变，锁定数量增加，可用数量减少</summary>
    public void Allocate(decimal quantity)
    {
        if (quantity <= 0) throw new BusinessException("Lock quantity must be greater than 0");
        if (AvailableQty < quantity)
            throw new BusinessException($"Insufficient stock: available {AvailableQty}, required {quantity}", ErrorCodes.InventoryInsufficient);

        LockedQty += quantity;
    }

    /// <summary>释放锁定：取消出库时调用</summary>
    public void Release(decimal quantity)
    {
        if (quantity <= 0) throw new BusinessException("Release quantity must be greater than 0");
        if (LockedQty < quantity)
            throw new BusinessException($"Release quantity exceeds locked quantity: locked {LockedQty}, releasing {quantity}", ErrorCodes.InvalidStatus);

        LockedQty -= quantity;
    }

    /// <summary>出库发运：同时扣减在库数量与锁定数量</summary>
    public void Ship(decimal quantity)
    {
        if (quantity <= 0) throw new BusinessException("Outbound quantity must be greater than 0");
        if (LockedQty < quantity)
            throw new BusinessException($"Insufficient locked quantity: locked {LockedQty}, required {quantity}", ErrorCodes.InventoryInsufficient);
        if (OnHandQty < quantity)
            throw new BusinessException($"Insufficient on-hand quantity: on hand {OnHandQty}, required {quantity}", ErrorCodes.InventoryInsufficient);

        OnHandQty -= quantity;
        LockedQty -= quantity;
        LastOutAt = DateTime.Now;
    }

    /// <summary>直接扣减在库数量（调拨出库等场景，要求可用数量充足）</summary>
    public void Deduct(decimal quantity)
    {
        if (quantity <= 0) throw new BusinessException("Deduction quantity must be greater than 0");
        if (AvailableQty < quantity)
            throw new BusinessException($"Insufficient available stock: available {AvailableQty}, required {quantity}", ErrorCodes.InventoryInsufficient);

        OnHandQty -= quantity;
        LastOutAt = DateTime.Now;
    }

    /// <summary>盘点调整：将账面数量调整为目标数量</summary>
    /// <returns>调整差异（正数为盘盈，负数为盘亏）</returns>
    public decimal AdjustTo(decimal targetOnHandQty)
    {
        if (targetOnHandQty < 0) throw new BusinessException("Stocktake quantity cannot be negative");
        if (LockedQty > targetOnHandQty)
            throw new BusinessException($"Stocktake quantity cannot be less than the locked quantity {LockedQty}", ErrorCodes.InventoryLocked);

        var diff = targetOnHandQty - OnHandQty;
        OnHandQty = targetOnHandQty;
        return diff;
    }
}
