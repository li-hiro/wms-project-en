using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;
using Wms.WebApi.Authorization;

namespace Wms.WebApi.Controllers;

/// <summary>库存盘点接口</summary>
[ApiController]
[Route("api/stocktake-orders")]
public class StockTakeController : ControllerBase
{
    private readonly StockTakeService _stockTakeService;

    public StockTakeController(StockTakeService stockTakeService)
    {
        _stockTakeService = stockTakeService;
    }

    /// <summary>分页查询盘点单</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.StockTakeView)]
    public async Task<ApiResult<PagedResult<StockTakeDto>>> Query([FromQuery] StockTakeQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<StockTakeDto>>.Ok(await _stockTakeService.QueryAsync(query, cancellationToken));

    /// <summary>获取盘点单详情（含明细）</summary>
    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.StockTakeView)]
    public async Task<ApiResult<StockTakeDto>> Get(long id, CancellationToken cancellationToken)
        => ApiResult<StockTakeDto>.Ok(await _stockTakeService.GetAsync(id, cancellationToken));

    /// <summary>创建盘点单（按当前库存自动生成账面快照）</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.StockTakeEdit)]
    public async Task<ApiResult<long>> Create([FromBody] CreateStockTakeRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _stockTakeService.CreateAsync(request, cancellationToken), "Stocktake order created successfully");

    /// <summary>录入实盘数量</summary>
    [HttpPut("{id:long}/items")]
    [HasPermission(PermissionCodes.StockTakeEdit)]
    public async Task<ApiResult<object>> UpdateItems(long id, [FromBody] UpdateStockTakeItemsRequest request, CancellationToken cancellationToken)
    {
        await _stockTakeService.UpdateItemsAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Counted quantities saved");
    }

    /// <summary>完成盘点并按差异调整库存</summary>
    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionCodes.StockTakeExecute)]
    public async Task<ApiResult<object>> Complete(long id, CancellationToken cancellationToken)
    {
        await _stockTakeService.CompleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Stocktake completed; inventory adjusted for variances");
    }

    /// <summary>取消盘点单</summary>
    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionCodes.StockTakeEdit)]
    public async Task<ApiResult<object>> Cancel(long id, CancellationToken cancellationToken)
    {
        await _stockTakeService.CancelAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Stocktake order cancelled");
    }

    /// <summary>删除盘点单（仅待盘点状态）</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.StockTakeEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _stockTakeService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Stocktake order deleted");
    }
}

/// <summary>库存调拨接口</summary>
[ApiController]
[Route("api/transfer-orders")]
public class TransferController : ControllerBase
{
    private readonly TransferService _transferService;

    public TransferController(TransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>分页查询调拨单</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.TransferView)]
    public async Task<ApiResult<PagedResult<TransferOrderDto>>> Query([FromQuery] TransferQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<TransferOrderDto>>.Ok(await _transferService.QueryAsync(query, cancellationToken));

    /// <summary>获取调拨单详情（含明细）</summary>
    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.TransferView)]
    public async Task<ApiResult<TransferOrderDto>> Get(long id, CancellationToken cancellationToken)
        => ApiResult<TransferOrderDto>.Ok(await _transferService.GetAsync(id, cancellationToken));

    /// <summary>创建调拨单（草稿）</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.TransferEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveTransferRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _transferService.CreateAsync(request, cancellationToken), "Transfer order created successfully");

    /// <summary>更新调拨单（仅草稿）</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.TransferEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveTransferRequest request, CancellationToken cancellationToken)
    {
        await _transferService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Transfer order updated successfully");
    }

    /// <summary>执行调拨（扣减调出库位并增加调入库位库存）</summary>
    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionCodes.TransferExecute)]
    public async Task<ApiResult<object>> Complete(long id, CancellationToken cancellationToken)
    {
        await _transferService.CompleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Transfer completed; stock moved");
    }

    /// <summary>取消调拨单</summary>
    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionCodes.TransferEdit)]
    public async Task<ApiResult<object>> Cancel(long id, CancellationToken cancellationToken)
    {
        await _transferService.CancelAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Transfer order cancelled");
    }

    /// <summary>删除调拨单（仅草稿）</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.TransferEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _transferService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Transfer order deleted");
    }
}
