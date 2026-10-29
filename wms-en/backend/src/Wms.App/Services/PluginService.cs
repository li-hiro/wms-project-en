using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Plugin;
using Wms.Plugin.BuiltIn;

namespace Wms.App.Services;

/// <summary>
/// 插件应用服务：桥接插件框架与业务数据，负责插件配置持久化与执行载荷组装。
/// </summary>
public class PluginService
{
    private readonly WmsDbContext _db;
    private readonly PluginManager _pluginManager;
    private readonly ILogger<PluginService> _logger;

    public PluginService(WmsDbContext db, PluginManager pluginManager, ILogger<PluginService> logger)
    {
        _db = db;
        _pluginManager = pluginManager;
        _logger = logger;
    }

    /// <summary>查询插件列表（合并已加载插件与数据库配置）</summary>
    public async Task<List<PluginDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _db.PluginConfigs.ToDictionaryAsync(x => x.PluginId, cancellationToken);

        return _pluginManager.GetAll().Select(plugin =>
        {
            var config = configs.GetValueOrDefault(plugin.Id);
            return new PluginDto
            {
                Id = plugin.Id,
                Name = plugin.Name,
                Description = plugin.Description,
                Category = plugin.Category,
                Version = plugin.Version,
                Author = plugin.Author,
                Enabled = config?.Enabled ?? true,
                Settings = ParseSettings(config?.SettingsJson)
            };
        }).ToList();
    }

    /// <summary>更新插件启用状态与配置</summary>
    public async Task UpdateAsync(string pluginId, UpdatePluginRequest request, CancellationToken cancellationToken = default)
    {
        var plugin = _pluginManager.Get(pluginId)
                     ?? throw new NotFoundException($"Plugin {pluginId} not found");

        var config = await _db.PluginConfigs.FirstOrDefaultAsync(x => x.PluginId == pluginId, cancellationToken);
        if (config == null)
        {
            config = new PluginConfig { PluginId = pluginId, Name = plugin.Name };
            _db.PluginConfigs.Add(config);
        }

        config.Enabled = request.Enabled;
        config.SettingsJson = JsonSerializer.Serialize(request.Settings);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>执行插件</summary>
    public async Task<PluginExecutionResult> ExecuteAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var plugin = _pluginManager.Get(pluginId)
                     ?? throw new NotFoundException($"Plugin {pluginId} not found");

        var config = await _db.PluginConfigs.FirstOrDefaultAsync(x => x.PluginId == pluginId, cancellationToken);
        if (config is { Enabled: false })
            throw new BusinessException("This plugin is disabled. Enable it first", ErrorCodes.InvalidStatus);

        var context = new PluginContext
        {
            Settings = ParseSettings(config?.SettingsJson),
            Payload = await BuildPayloadAsync(pluginId, cancellationToken),
            Logger = _logger
        };

        var result = await plugin.ExecuteAsync(context, cancellationToken);
        _logger.LogInformation("Plugin {PluginId} execution completed, result: {Success}", pluginId, result.Success);
        return result;
    }

    /// <summary>根据插件类型组装业务载荷</summary>
    private async Task<object?> BuildPayloadAsync(string pluginId, CancellationToken cancellationToken)
    {
        switch (pluginId)
        {
            case "inventory-alert":
                return await BuildAlertItemsAsync(cancellationToken);

            case "webhook-notify":
                return new
                {
                    eventType = "inventory.alert",
                    message = "WMS Stock Alert Notification",
                    sentAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    alerts = await BuildAlertItemsAsync(cancellationToken)
                };

            case "erp-sync":
            {
                var materials = await _db.Materials
                    .Where(x => x.Status == CommonStatus.Enabled)
                    .Take(200)
                    .ToListAsync(cancellationToken);

                return new ErpSyncPayload
                {
                    EntityType = "material",
                    RecordCount = materials.Count,
                    Records = materials.Select(x => (object)new
                    {
                        x.Code,
                        x.Name,
                        x.Spec,
                        x.Unit,
                        x.Price
                    }).ToList()
                };
            }

            default:
                return null;
        }
    }

    private async Task<List<InventoryAlertItem>> BuildAlertItemsAsync(CancellationToken cancellationToken)
    {
        var alerts = await BuildAlertsAsync(cancellationToken);
        return alerts.Select(x => new InventoryAlertItem
        {
            MaterialCode = x.MaterialCode,
            MaterialName = x.MaterialName,
            WarehouseName = x.WarehouseName,
            OnHandQty = x.OnHandQty,
            SafetyStock = x.SafetyStock,
            Unit = x.Unit
        }).ToList();
    }

    private async Task<List<Dtos.AlertItemDto>> BuildAlertsAsync(CancellationToken cancellationToken)
    {
        var materials = await _db.Materials.Where(x => x.SafetyStock > 0).ToListAsync(cancellationToken);
        var records = await _db.InventoryRecords.ToListAsync(cancellationToken);
        var warehouses = await _db.Warehouses.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var result = new List<Dtos.AlertItemDto>();
        foreach (var material in materials)
        {
            foreach (var warehouse in warehouses)
            {
                var onHand = records
                    .Where(x => x.MaterialId == material.Id && x.WarehouseId == warehouse.Key)
                    .Sum(x => x.OnHandQty);

                if (onHand >= material.SafetyStock)
                    continue;

                result.Add(new Dtos.AlertItemDto
                {
                    MaterialId = material.Id,
                    MaterialCode = material.Code,
                    MaterialName = material.Name,
                    Spec = material.Spec,
                    Unit = material.Unit,
                    WarehouseId = warehouse.Key,
                    WarehouseName = warehouse.Value,
                    OnHandQty = onHand,
                    SafetyStock = material.SafetyStock,
                    ShortageQty = material.SafetyStock - onHand
                });
            }
        }

        return result;
    }

    private static Dictionary<string, string> ParseSettings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
