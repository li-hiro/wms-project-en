using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;
using Wms.Plugin;
using Wms.WebApi.Authorization;

namespace Wms.WebApi.Controllers;

/// <summary>插件管理接口：查看、配置与执行扩展插件</summary>
[ApiController]
[Route("api/plugins")]
public class PluginController : ControllerBase
{
    private readonly PluginService _pluginService;

    public PluginController(PluginService pluginService)
    {
        _pluginService = pluginService;
    }

    /// <summary>查询已加载插件及其配置</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.PluginView)]
    public async Task<ApiResult<List<PluginDto>>> List(CancellationToken cancellationToken)
        => ApiResult<List<PluginDto>>.Ok(await _pluginService.ListAsync(cancellationToken));

    /// <summary>更新插件启用状态与配置</summary>
    [HttpPut("{pluginId}")]
    [HasPermission(PermissionCodes.PluginEdit)]
    public async Task<ApiResult<object>> Update(string pluginId, [FromBody] UpdatePluginRequest request, CancellationToken cancellationToken)
    {
        await _pluginService.UpdateAsync(pluginId, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Plugin configuration saved");
    }

    /// <summary>执行插件</summary>
    [HttpPost("{pluginId}/execute")]
    [HasPermission(PermissionCodes.PluginEdit)]
    public async Task<ApiResult<PluginExecutionResult>> Execute(string pluginId, CancellationToken cancellationToken)
        => ApiResult<PluginExecutionResult>.Ok(await _pluginService.ExecuteAsync(pluginId, cancellationToken), "Plugin execution completed");
}
