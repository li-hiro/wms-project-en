using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;
using Wms.WebApi.Authorization;

namespace Wms.WebApi.Controllers;

/// <summary>仓库配置接口</summary>
[ApiController]
[Route("api/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly WarehouseService _warehouseService;

    public WarehousesController(WarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    /// <summary>分页查询仓库</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.WarehouseView)]
    public async Task<ApiResult<PagedResult<WarehouseDto>>> Query([FromQuery] WarehouseQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<WarehouseDto>>.Ok(await _warehouseService.QueryAsync(query, cancellationToken));

    /// <summary>仓库下拉选项</summary>
    [HttpGet("options")]
    [HasPermission(PermissionCodes.WarehouseView)]
    public async Task<ApiResult<List<OptionDto>>> Options(CancellationToken cancellationToken)
        => ApiResult<List<OptionDto>>.Ok(await _warehouseService.OptionsAsync(cancellationToken));

    /// <summary>创建仓库</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.WarehouseEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveWarehouseRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _warehouseService.CreateAsync(request, cancellationToken), "Warehouse created successfully");

    /// <summary>更新仓库</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.WarehouseEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveWarehouseRequest request, CancellationToken cancellationToken)
    {
        await _warehouseService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Warehouse updated successfully");
    }

    /// <summary>删除仓库</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.WarehouseEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Warehouse deleted successfully");
    }
}

/// <summary>库位接口</summary>
[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly LocationService _locationService;

    public LocationsController(LocationService locationService)
    {
        _locationService = locationService;
    }

    /// <summary>分页查询库位</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.LocationView)]
    public async Task<ApiResult<PagedResult<LocationDto>>> Query([FromQuery] LocationQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<LocationDto>>.Ok(await _locationService.QueryAsync(query, cancellationToken));

    /// <summary>按仓库查询可用库位（下拉选项）</summary>
    [HttpGet("options")]
    [HasPermission(PermissionCodes.LocationView)]
    public async Task<ApiResult<List<OptionDto>>> Options([FromQuery] long? warehouseId, CancellationToken cancellationToken)
        => ApiResult<List<OptionDto>>.Ok(await _locationService.OptionsAsync(warehouseId, cancellationToken));

    /// <summary>创建库位</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.LocationEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveLocationRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _locationService.CreateAsync(request, cancellationToken), "Location created successfully");

    /// <summary>更新库位</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.LocationEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveLocationRequest request, CancellationToken cancellationToken)
    {
        await _locationService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Location updated successfully");
    }

    /// <summary>删除库位</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.LocationEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _locationService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Location deleted successfully");
    }
}
