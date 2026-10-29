using Wms.Core.Common;

namespace Wms.App.Entities;

/// <summary>操作日志</summary>
public class OperationLog : BaseEntity
{
    /// <summary>操作人 ID</summary>
    public long? UserId { get; set; }

    /// <summary>操作人账号</summary>
    public string? UserName { get; set; }

    /// <summary>业务模块</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>操作描述</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>HTTP 方法</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>请求路径</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>客户端 IP</summary>
    public string? Ip { get; set; }

    /// <summary>请求参数（已截断）</summary>
    public string? Params { get; set; }

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>耗时（毫秒）</summary>
    public long DurationMs { get; set; }
}

/// <summary>插件配置</summary>
public class PluginConfig : BaseEntity
{
    /// <summary>插件标识</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>插件名称快照</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>配置项（JSON 序列化存储）</summary>
    public string SettingsJson { get; set; } = "{}";
}
