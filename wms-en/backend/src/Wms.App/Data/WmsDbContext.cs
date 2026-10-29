using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Entities;
using Wms.Im.Entities;

namespace Wms.App.Data;

/// <summary>
/// 系统数据上下文：统一管理基础资料、权限体系、作业单据与库存台账。
/// </summary>
public class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options) : base(options)
    {
    }

    // 系统权限
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();

    // 基础资料
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();

    // 作业单据
    public DbSet<InboundOrder> InboundOrders => Set<InboundOrder>();
    public DbSet<InboundOrderItem> InboundOrderItems => Set<InboundOrderItem>();
    public DbSet<OutboundOrder> OutboundOrders => Set<OutboundOrder>();
    public DbSet<OutboundOrderItem> OutboundOrderItems => Set<OutboundOrderItem>();
    public DbSet<StockTakeOrder> StockTakeOrders => Set<StockTakeOrder>();
    public DbSet<StockTakeItem> StockTakeItems => Set<StockTakeItem>();
    public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();
    public DbSet<TransferOrderItem> TransferOrderItems => Set<TransferOrderItem>();

    // 库存
    public DbSet<InventoryRecord> InventoryRecords => Set<InventoryRecord>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    // 系统辅助
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<PluginConfig> PluginConfigs => Set<PluginConfig>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite 对 decimal 的比较与聚合支持有限，统一映射为 double，保证跨数据库行为一致
        configurationBuilder.Properties<decimal>().HaveConversion<double>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- 唯一索引 ----------
        modelBuilder.Entity<User>().HasIndex(x => x.UserName).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<UserRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        modelBuilder.Entity<RoleMenu>().HasIndex(x => new { x.RoleId, x.MenuId }).IsUnique();

        modelBuilder.Entity<Material>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Warehouse>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<WarehouseLocation>().HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique();

        modelBuilder.Entity<InboundOrder>().HasIndex(x => x.OrderNo).IsUnique();
        modelBuilder.Entity<OutboundOrder>().HasIndex(x => x.OrderNo).IsUnique();
        modelBuilder.Entity<StockTakeOrder>().HasIndex(x => x.OrderNo).IsUnique();
        modelBuilder.Entity<TransferOrder>().HasIndex(x => x.OrderNo).IsUnique();
        modelBuilder.Entity<PluginConfig>().HasIndex(x => x.PluginId).IsUnique();

        modelBuilder.Entity<InventoryRecord>()
            .HasIndex(x => new { x.MaterialId, x.WarehouseId, x.LocationId, x.BatchNo })
            .IsUnique();
        modelBuilder.Entity<InventoryTransaction>().HasIndex(x => x.No).IsUnique();
        modelBuilder.Entity<InventoryTransaction>().HasIndex(x => x.OccurredAt);
        modelBuilder.Entity<InventoryTransaction>().HasIndex(x => new { x.MaterialId, x.WarehouseId });
        modelBuilder.Entity<OperationLog>().HasIndex(x => x.CreatedAt);

        // ---------- 单据与明细的级联关系 ----------
        modelBuilder.Entity<InboundOrder>()
            .HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OutboundOrder>()
            .HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockTakeOrder>()
            .HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TransferOrder>()
            .HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

        // ---------- 逻辑删除全局过滤 ----------
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var body = Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    /// <summary>统一维护审计时间戳</summary>
    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    /// <summary>统一维护审计时间戳</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.Now;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}
