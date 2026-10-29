using Microsoft.Extensions.Logging;

namespace Wms.Plugin;

/// <summary>
/// 插件执行上下文：由宿主注入配置、业务数据与日志组件，插件不直接依赖应用层。
/// </summary>
public class PluginContext
{
    /// <summary>插件配置项（来自数据库配置）</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

    /// <summary>宿主传入的业务数据，具体结构由插件自身约定</summary>
    public object? Payload { get; init; }

    /// <summary>日志组件</summary>
    public ILogger Logger { get; init; } = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
}

/// <summary>插件执行结果</summary>
public class PluginExecutionResult
{
    /// <summary>是否执行成功</summary>
    public bool Success { get; set; }

    /// <summary>结果说明</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>结构化结果数据</summary>
    public object? Data { get; set; }

    /// <summary>耗时（毫秒）</summary>
    public long DurationMs { get; set; }

    /// <summary>执行时间</summary>
    public DateTime ExecutedAt { get; set; } = DateTime.Now;

    public static PluginExecutionResult Ok(string message, object? data = null) => new() { Success = true, Message = message, Data = data };

    public static PluginExecutionResult Fail(string message) => new() { Success = false, Message = message };
}
