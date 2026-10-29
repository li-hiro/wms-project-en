using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;
using Wms.WebApi.Authorization;

namespace Wms.WebApi.Controllers;

/// <summary>库存查询接口：台账、流水、仓库汇总与预警</summary>
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryQueryService _inventoryQueryService;

    public InventoryController(InventoryQueryService inventoryQueryService)
    {
        _inventoryQueryService = inventoryQueryService;
    }

    /// <summary>分页查询库存台账</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.InventoryView)]
    public async Task<ApiResult<PagedResult<InventoryItemDto>>> Query([FromQuery] InventoryQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<InventoryItemDto>>.Ok(await _inventoryQueryService.QueryAsync(query, cancellationToken));

    /// <summary>分页查询库存流水</summary>
    [HttpGet("transactions")]
    [HasPermission(PermissionCodes.InventoryView)]
    public async Task<ApiResult<PagedResult<TransactionDto>>> Transactions([FromQuery] TransactionQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<TransactionDto>>.Ok(await _inventoryQueryService.TransactionsAsync(query, cancellationToken));

    /// <summary>按仓库汇总库存</summary>
    [HttpGet("summary")]
    [HasPermission(PermissionCodes.InventoryView)]
    public async Task<ApiResult<List<InventorySummaryDto>>> Summary(CancellationToken cancellationToken)
        => ApiResult<List<InventorySummaryDto>>.Ok(await _inventoryQueryService.SummaryAsync(cancellationToken));

    /// <summary>库存预警列表（低于安全库存）</summary>
    [HttpGet("alerts")]
    [HasPermission(PermissionCodes.InventoryView)]
    public async Task<ApiResult<List<AlertItemDto>>> Alerts(CancellationToken cancellationToken)
        => ApiResult<List<AlertItemDto>>.Ok(await _inventoryQueryService.AlertsAsync(cancellationToken));

    /// <summary>最近库存流水（工作台使用）</summary>
    [HttpGet("recent")]
    [HasPermission(PermissionCodes.InventoryView)]
    public async Task<ApiResult<List<TransactionDto>>> Recent([FromQuery] int take = 8, CancellationToken cancellationToken = default)
        => ApiResult<List<TransactionDto>>.Ok(await _inventoryQueryService.RecentAsync(take, cancellationToken));
}

/// <summary>报表与工作台接口</summary>
[ApiController]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportController(ReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>工作台概览数据</summary>
    [HttpGet("dashboard")]
    [HasPermission(PermissionCodes.DashboardView)]
    public async Task<ApiResult<DashboardDto>> Dashboard(CancellationToken cancellationToken)
        => ApiResult<DashboardDto>.Ok(await _reportService.DashboardAsync(cancellationToken));

    /// <summary>库存汇总报表</summary>
    [HttpGet("stock-summary")]
    [HasPermission(PermissionCodes.ReportView)]
    public async Task<ApiResult<List<StockSummaryItem>>> StockSummary([FromQuery] long? warehouseId, CancellationToken cancellationToken)
        => ApiResult<List<StockSummaryItem>>.Ok(await _reportService.StockSummaryAsync(warehouseId, cancellationToken));

    /// <summary>收发存报表</summary>
    [HttpGet("in-out-summary")]
    [HasPermission(PermissionCodes.ReportView)]
    public async Task<ApiResult<List<InOutSummaryItem>>> InOutSummary(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] long? warehouseId,
        CancellationToken cancellationToken)
        => ApiResult<List<InOutSummaryItem>>.Ok(await _reportService.InOutSummaryAsync(startDate, endDate, warehouseId, cancellationToken));

    /// <summary>出入库趋势</summary>
    [HttpGet("trend")]
    [HasPermission(PermissionCodes.ReportView)]
    public async Task<ApiResult<List<TrendItem>>> Trend([FromQuery] int days = 7, CancellationToken cancellationToken = default)
        => ApiResult<List<TrendItem>>.Ok(await _reportService.TrendAsync(days, cancellationToken));

    /// <summary>出库量 TOP 物料</summary>
    [HttpGet("top-materials")]
    [HasPermission(PermissionCodes.ReportView)]
    public async Task<ApiResult<List<TopMaterialItem>>> TopMaterials(
        [FromQuery] int days = 30,
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
        => ApiResult<List<TopMaterialItem>>.Ok(await _reportService.TopMaterialsAsync(days, count, cancellationToken));
}
