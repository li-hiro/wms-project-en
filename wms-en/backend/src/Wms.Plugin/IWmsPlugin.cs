namespace Wms.Plugin;

/// <summary>
/// WMS 插件契约。任何实现该接口的类都可以作为插件被加载，
/// 既可以是内置插件，也可以编译为独立 DLL 后放入插件目录动态加载。
/// </summary>
public interface IWmsPlugin
{
    /// <summary>插件唯一标识</summary>
    string Id { get; }

    /// <summary>插件名称</summary>
    string Name { get; }

    /// <summary>插件描述</summary>
    string Description { get; }

    /// <summary>插件分类，如「库存增强」「第三方集成」</summary>
    string Category { get; }

    /// <summary>插件版本号</summary>
    string Version { get; }

    /// <summary>插件作者</summary>
    string Author { get; }

    /// <summary>执行插件逻辑</summary>
    Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default);
}
