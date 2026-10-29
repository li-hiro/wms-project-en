using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Wms.Plugin.BuiltIn;

/// <summary>
/// Webhook 通知插件：将宿主传入的业务数据以 JSON 形式推送到第三方系统（如企业微信机器人、钉钉机器人）。
/// 配置项：webhookUrl（必填）、secret（可选，预留签名扩展）。
/// </summary>
public class WebhookNotifyPlugin : IWmsPlugin
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public string Id => "webhook-notify";
    public string Name => "Webhook Notification";
    public string Description => "Pushes events such as stock alerts and job completion to a third-party webhook URL as JSON.";
    public string Category => "Third-party Integration";
    public string Version => "1.0.0";
    public string Author => "WMS Team";

    public async Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var url = context.Settings.TryGetValue("webhookUrl", out var value) ? value : null;
        if (string.IsNullOrWhiteSpace(url))
            return PluginExecutionResult.Fail("webhookUrl is not configured. Please set the target URL in the plugin settings");

        try
        {
            var response = await HttpClient.PostAsJsonAsync(url, new
            {
                source = "WMS",
                sentAt = DateTime.Now,
                data = context.Payload
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return PluginExecutionResult.Fail($"Push failed, HTTP status {response.StatusCode}");

            context.Logger.LogInformation("Webhook push succeeded: {Url}", url);
            return PluginExecutionResult.Ok("Webhook push succeeded", new { statusCode = (int)response.StatusCode });
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "Webhook push failed: {Url}", url);
            return PluginExecutionResult.Fail($"Push error: {ex.Message}");
        }
    }
}
