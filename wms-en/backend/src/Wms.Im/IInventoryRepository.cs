using Wms.Im.Entities;

namespace Wms.Im;

/// <summary>
/// 库存持久化抽象：由应用层基于 EF Core 实现，使库存领域逻辑与具体 ORM 解耦。
/// </summary>
public interface IInventoryRepository
{
    /// <summary>按库存维度查找台账记录</summary>
    Task<InventoryRecord?> FindAsync(long materialId, long warehouseId, long locationId, string batchNo, CancellationToken cancellationToken = default);

    /// <summary>查询指定仓库的全部库存台账</summary>
    Task<IReadOnlyList<InventoryRecord>> ListByWarehouseAsync(long warehouseId, CancellationToken cancellationToken = default);

    /// <summary>新增库存台账</summary>
    InventoryRecord AddRecord(InventoryRecord record);

    /// <summary>新增库存流水</summary>
    void AddTransaction(InventoryTransaction transaction);

    /// <summary>提交工作单元</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
