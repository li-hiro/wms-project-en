using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wms.App.Data;
using Wms.App.Services;
using Wms.Im;

namespace Wms.App;

/// <summary>
/// 应用层服务注册入口：集中装配数据上下文、仓储实现与业务服务。
/// </summary>
public static class AppModuleExtensions
{
    /// <summary>注册应用层全部服务</summary>
    public static IServiceCollection AddApplicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=wms.db";

        services.AddDbContext<WmsDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.EnableDetailedErrors();
        });

        // 库存领域仓储适配
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        // 权限与认证
        services.AddScoped<UserAccessService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<RoleService>();
        services.AddScoped<MenuService>();

        // 基础资料与仓库
        services.AddScoped<CategoryService>();
        services.AddScoped<MaterialService>();
        services.AddScoped<SupplierService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<WarehouseService>();
        services.AddScoped<LocationService>();

        // 作业单据
        services.AddScoped<InboundService>();
        services.AddScoped<OutboundService>();
        services.AddScoped<StockTakeService>();
        services.AddScoped<TransferService>();

        // 库存与报表
        services.AddScoped<InventoryQueryService>();
        services.AddScoped<ReportService>();
        services.AddScoped<PluginService>();
        services.AddScoped<OperationLogService>();

        return services;
    }
}
