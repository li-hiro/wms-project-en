using Microsoft.Extensions.DependencyInjection;

namespace Wms.Plugin;

/// <summary>插件模块注册入口</summary>
public static class PluginModuleExtensions
{
    /// <summary>注册插件管理器（单例，进程内共享插件注册表）</summary>
    public static IServiceCollection AddPluginModule(this IServiceCollection services)
    {
        services.AddSingleton<PluginManager>();
        return services;
    }
}
