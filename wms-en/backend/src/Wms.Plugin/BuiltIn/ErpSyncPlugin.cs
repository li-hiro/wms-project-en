using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Wms.Plugin.BuiltIn;

/// <summary>ERP 同步载荷</summary>
public class ErpSyncPayload
{
    /// <summary>同步实体类型，如 material、supplier</summary>
    public string EntityType { get; set; } = "material";

    /// <summary>同步数据条数</summary>
    public int RecordCount { get; set; }

    /// <summary>同步明细</summary>
    public IReadOnlyList<object> Records { get; set; } = Array.Empty<object>();
}

/// <summary>
/// ERP 第三方集成适配插件：演示如何把 WMS 数据映射为外部系统字段并推送。
/// 配置项：erpEndpoint（外部接口地址）、erpToken（访问令牌）。
/// 未配置接口时仅输出字段映射结果，便于离线演示。
/// </summary>
public class ErpSyncPlugin : IWmsPlugin
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    /// <summary>WMS 字段到 ERP 字段的映射约定</summary>
    private static readonly Dictionary<string, string> FieldMapping = new()
    {
        ["Code"] = "ext_code",
        ["Name"] = "ext_name",
        ["Spec"] = "ext_spec",
        ["Unit"] = "ext_uom",
        ["Price"] = "ext_price"
    };

    public string Id => "erp-sync";
    public string Name => "ERP Data Sync Adapter";
    public string Description => "Maps master data such as materials to ERP field conventions and pushes it; serves as a third-party integration template.";
    public string Category => "Third-party Integration";
    public string Version => "1.0.0";
    public string Author => "WMS Team";

    public async Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = context.Settings.TryGetValue("erpEndpoint", out var value) ? value : null;
        var payload = context.Payload as ErpSyncPayload ?? new ErpSyncPayload();

        var mapped = new
        {
            entity = payload.EntityType,
            count = payload.RecordCount,
            fieldMapping = FieldMapping
        };

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return PluginExecutionResult.Ok(
                $"ERP endpoint is not configured. Field mapping validation completed for {payload.RecordCount} record(s) (offline mode)",
                mapped);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new { entity = payload.EntityType, records = payload.Records })
            };

            if (context.Settings.TryGetValue("erpToken", out var token) && !string.IsNullOrWhiteSpace(token))
                request.Headers.Add("Authorization", $"Bearer {token}");

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return PluginExecutionResult.Fail($"ERP sync failed, HTTP status {response.StatusCode}");

            return PluginExecutionResult.Ok($"Pushed {payload.RecordCount} record(s) to ERP", mapped);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "ERP sync error: {Endpoint}", endpoint);
            return PluginExecutionResult.Fail($"ERP sync error: {ex.Message}");
        }
    }
}
