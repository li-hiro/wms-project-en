using Microsoft.EntityFrameworkCore;
using Wms.Im;
using Wms.Im.Entities;

namespace Wms.App.Data;

/// <summary>
/// 基于 EF Core 的库存仓储实现，供库存领域引擎调用。
/// </summary>
public class InventoryRepository : IInventoryRepository
{
    private readonly WmsDbContext _db;

    public InventoryRepository(WmsDbContext db)
    {
        _db = db;
    }

    public Task<InventoryRecord?> FindAsync(
        long materialId,
        long warehouseId,
        long locationId,
        string batchNo,
        CancellationToken cancellationToken = default)
        => _db.InventoryRecords.FirstOrDefaultAsync(
            x => x.MaterialId == materialId
                 && x.WarehouseId == warehouseId
                 && x.LocationId == locationId
                 && x.BatchNo == batchNo,
            cancellationToken);

    public async Task<IReadOnlyList<InventoryRecord>> ListByWarehouseAsync(
        long warehouseId,
        CancellationToken cancellationToken = default)
        => await _db.InventoryRecords
            .Where(x => x.WarehouseId == warehouseId && x.OnHandQty > 0)
            .OrderBy(x => x.MaterialId)
            .ToListAsync(cancellationToken);

    public InventoryRecord AddRecord(InventoryRecord record)
    {
        _db.InventoryRecords.Add(record);
        return record;
    }

    public void AddTransaction(InventoryTransaction transaction)
    {
        _db.InventoryTransactions.Add(transaction);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
