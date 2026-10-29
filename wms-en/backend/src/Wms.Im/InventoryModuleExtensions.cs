using Microsoft.Extensions.DependencyInjection;

namespace Wms.Im;

/// <summary>
/// 库存模块的服务注册入口，Web 宿主只需调用 AddInventoryModule 即可装配库存域。
/// </summary>
public static class InventoryModuleExtensions
{
    /// <summary>注册库存管理核心服务</summary>
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<InventoryManager>();
        return services;
    }
}
