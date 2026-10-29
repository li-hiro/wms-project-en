using Microsoft.Extensions.Logging;

namespace Wms.Plugin.BuiltIn;

/// <summary>库存预警插件输入项</summary>
public class InventoryAlertItem
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal OnHandQty { get; set; }
    public decimal SafetyStock { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// 库存预警插件：对低于安全库存的物料进行汇总，供首页与通知渠道使用。
/// </summary>
public class InventoryAlertPlugin : IWmsPlugin
{
    public string Id => "inventory-alert";
    public string Name => "Safety Stock Alert";
    public string Description => "Scans current inventory and lists materials below safety stock; can feed in-app reminders or third-party notifications.";
    public string Category => "Inventory Enhancement";
    public string Version => "1.0.0";
    public string Author => "WMS Team";

    public Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        if (context.Payload is not IEnumerable<InventoryAlertItem> items)
            return Task.FromResult(PluginExecutionResult.Fail("Invalid payload format, expected a collection of InventoryAlertItem"));

        var alerts = items
            .Where(x => x.OnHandQty < x.SafetyStock)
            .OrderBy(x => x.OnHandQty - x.SafetyStock)
            .Select(x => new
            {
                x.MaterialCode,
                x.MaterialName,
                x.WarehouseName,
                x.OnHandQty,
                x.SafetyStock,
                x.Unit,
                ShortageQty = x.SafetyStock - x.OnHandQty
            })
            .ToList();

        context.Logger.LogInformation("Inventory alert plugin finished, {Count} alerts found", alerts.Count);

        return Task.FromResult(PluginExecutionResult.Ok(
            alerts.Count == 0 ? "All materials are above safety stock" : $"{alerts.Count} stock alert(s) found",
            alerts));
    }
}
