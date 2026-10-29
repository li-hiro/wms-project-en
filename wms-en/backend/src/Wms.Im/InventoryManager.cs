using Microsoft.Extensions.Logging;
using Wms.Core.Common;
using Wms.Core.Utils;
using Wms.Im.Entities;

namespace Wms.Im;

/// <summary>
/// 库存引擎：统一封装入库、出库、锁定、释放、调拨与盘点调整等库存变动逻辑，
/// 每次变动都会生成库存流水，确保账实可追溯。
/// </summary>
public class InventoryManager
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<InventoryManager> _logger;

    public InventoryManager(IInventoryRepository repository, ILogger<InventoryManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>入库（采购/生产/退货/其他/调拨入/期初）</summary>
    public async Task<InventoryTransaction> IncreaseAsync(
        InventoryOperation operation,
        InventoryTransactionType type,
        CancellationToken cancellationToken = default)
    {
        EnsurePositive(operation);

        var record = await _repository.FindAsync(
            operation.MaterialId, operation.WarehouseId, operation.LocationId,
            operation.NormalizedBatchNo, cancellationToken);

        if (record == null)
        {
            record = _repository.AddRecord(new InventoryRecord
            {
                MaterialId = operation.MaterialId,
                WarehouseId = operation.WarehouseId,
                LocationId = operation.LocationId,
                BatchNo = operation.NormalizedBatchNo,
                OnHandQty = 0,
                LockedQty = 0,
                UnitCost = operation.UnitCost,
                CreatedBy = operation.OperatorId
            });
        }

        record.Increase(operation.Quantity, operation.UnitCost);
        var transaction = BuildTransaction(operation, type, operation.Quantity, 0, record);
        _repository.AddTransaction(transaction);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stock inbound {RefNo}: material {MaterialId} +{Qty}, balance {Balance}",
            operation.RefNo, operation.MaterialId, operation.Quantity, record.OnHandQty);
        return transaction;
    }

    /// <summary>锁定库存（出库单确认时调用）</summary>
    public async Task<InventoryTransaction> AllocateAsync(InventoryOperation operation, CancellationToken cancellationToken = default)
    {
        EnsurePositive(operation);
        var record = await GetRequiredAsync(operation, cancellationToken);
        record.Allocate(operation.Quantity);

        var transaction = BuildTransaction(operation, InventoryTransactionType.Lock, 0, operation.Quantity, record);
        _repository.AddTransaction(transaction);
        await _repository.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <summary>释放锁定库存（出库单取消时调用）</summary>
    public async Task<InventoryTransaction> ReleaseAsync(InventoryOperation operation, CancellationToken cancellationToken = default)
    {
        EnsurePositive(operation);
        var record = await GetRequiredAsync(operation, cancellationToken);
        record.Release(operation.Quantity);

        var transaction = BuildTransaction(operation, InventoryTransactionType.Unlock, 0, -operation.Quantity, record);
        _repository.AddTransaction(transaction);
        await _repository.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <summary>出库发运（扣减在库与锁定数量）</summary>
    public async Task<InventoryTransaction> ShipAsync(InventoryOperation operation, CancellationToken cancellationToken = default)
    {
        EnsurePositive(operation);
        var record = await GetRequiredAsync(operation, cancellationToken);
        record.Ship(operation.Quantity);

        var transaction = BuildTransaction(operation, InventoryTransactionType.Outbound, -operation.Quantity, -operation.Quantity, record);
        _repository.AddTransaction(transaction);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stock outbound {RefNo}: material {MaterialId} -{Qty}, balance {Balance}",
            operation.RefNo, operation.MaterialId, operation.Quantity, record.OnHandQty);
        return transaction;
    }

    /// <summary>调拨出库：直接扣减来源库位可用库存</summary>
    public async Task<InventoryTransaction> TransferOutAsync(InventoryOperation operation, CancellationToken cancellationToken = default)
    {
        EnsurePositive(operation);
        var record = await GetRequiredAsync(operation, cancellationToken);
        record.Deduct(operation.Quantity);

        var transaction = BuildTransaction(operation, InventoryTransactionType.TransferOut, -operation.Quantity, 0, record);
        _repository.AddTransaction(transaction);
        await _repository.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    /// <summary>盘点调整：将账面数量调整为实盘数量并记录差异</summary>
    /// <returns>调整流水，无差异时返回 null</returns>
    public async Task<InventoryTransaction?> AdjustAsync(
        InventoryOperation operation,
        decimal targetOnHandQty,
        CancellationToken cancellationToken = default)
    {
        var record = await GetRequiredAsync(operation, cancellationToken);
        var diff = record.AdjustTo(targetOnHandQty);
        if (diff == 0)
            return null;

        var adjustOperation = operation.With(Math.Abs(diff));
        var transaction = BuildTransaction(adjustOperation, InventoryTransactionType.Adjust, diff, 0, record);
        _repository.AddTransaction(transaction);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stocktake adjustment {RefNo}: material {MaterialId} diff {Diff}, balance {Balance}",
            operation.RefNo, operation.MaterialId, diff, record.OnHandQty);
        return transaction;
    }

    private async Task<InventoryRecord> GetRequiredAsync(InventoryOperation operation, CancellationToken cancellationToken)
    {
        var record = await _repository.FindAsync(
            operation.MaterialId, operation.WarehouseId, operation.LocationId,
            operation.NormalizedBatchNo, cancellationToken);

        if (record == null)
            throw new BusinessException("Inventory record not found. Please verify material, warehouse, location and batch", ErrorCodes.InventoryInsufficient);

        return record;
    }

    private static void EnsurePositive(InventoryOperation operation)
    {
        if (operation.Quantity <= 0)
            throw new BusinessException("Inventory operation quantity must be greater than 0");
    }

    private static InventoryTransaction BuildTransaction(
        InventoryOperation operation,
        InventoryTransactionType type,
        decimal quantity,
        decimal lockedQuantity,
        InventoryRecord record) => new()
    {
        No = OrderNoGenerator.GenerateTransactionNo(),
        MaterialId = operation.MaterialId,
        WarehouseId = operation.WarehouseId,
        LocationId = operation.LocationId,
        BatchNo = operation.NormalizedBatchNo,
        Type = type,
        Quantity = quantity,
        LockedQuantity = lockedQuantity,
        BalanceQty = record.OnHandQty,
        LockedBalanceQty = record.LockedQty,
        RefType = operation.RefType,
        RefId = operation.RefId,
        RefNo = operation.RefNo,
        Remark = operation.Remark,
        OccurredAt = DateTime.Now,
        OperatorId = operation.OperatorId,
        OperatorName = operation.OperatorName,
        CreatedBy = operation.OperatorId
    };
}
