using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;
using Wms.WebApi.Authorization;

namespace Wms.WebApi.Controllers;

/// <summary>入库作业接口</summary>
[ApiController]
[Route("api/inbound-orders")]
public class InboundController : ControllerBase
{
    private readonly InboundService _inboundService;

    public InboundController(InboundService inboundService)
    {
        _inboundService = inboundService;
    }

    /// <summary>分页查询入库单</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.InboundView)]
    public async Task<ApiResult<PagedResult<InboundOrderDto>>> Query([FromQuery] InboundQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<InboundOrderDto>>.Ok(await _inboundService.QueryAsync(query, cancellationToken));

    /// <summary>获取入库单详情（含明细）</summary>
    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.InboundView)]
    public async Task<ApiResult<InboundOrderDto>> Get(long id, CancellationToken cancellationToken)
        => ApiResult<InboundOrderDto>.Ok(await _inboundService.GetAsync(id, cancellationToken));

    /// <summary>创建入库单（草稿）</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.InboundEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveInboundRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _inboundService.CreateAsync(request, cancellationToken), "Inbound order created successfully");

    /// <summary>更新入库单（仅草稿）</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.InboundEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveInboundRequest request, CancellationToken cancellationToken)
    {
        await _inboundService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Inbound order updated successfully");
    }

    /// <summary>确认入库单</summary>
    [HttpPost("{id:long}/confirm")]
    [HasPermission(PermissionCodes.InboundEdit)]
    public async Task<ApiResult<object>> Confirm(long id, CancellationToken cancellationToken)
    {
        await _inboundService.ConfirmAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Inbound order confirmed");
    }

    /// <summary>执行入库（增加库存并生成流水）</summary>
    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionCodes.InboundExecute)]
    public async Task<ApiResult<object>> Complete(long id, CancellationToken cancellationToken)
    {
        await _inboundService.CompleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Inbound completed; inventory updated");
    }

    /// <summary>取消入库单</summary>
    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionCodes.InboundEdit)]
    public async Task<ApiResult<object>> Cancel(long id, CancellationToken cancellationToken)
    {
        await _inboundService.CancelAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Inbound order cancelled");
    }

    /// <summary>删除入库单（仅草稿）</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.InboundEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _inboundService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Inbound order deleted");
    }
}

/// <summary>出库作业接口</summary>
[ApiController]
[Route("api/outbound-orders")]
public class OutboundController : ControllerBase
{
    private readonly OutboundService _outboundService;

    public OutboundController(OutboundService outboundService)
    {
        _outboundService = outboundService;
    }

    /// <summary>分页查询出库单</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.OutboundView)]
    public async Task<ApiResult<PagedResult<OutboundOrderDto>>> Query([FromQuery] OutboundQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<OutboundOrderDto>>.Ok(await _outboundService.QueryAsync(query, cancellationToken));

    /// <summary>获取出库单详情（含明细）</summary>
    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.OutboundView)]
    public async Task<ApiResult<OutboundOrderDto>> Get(long id, CancellationToken cancellationToken)
        => ApiResult<OutboundOrderDto>.Ok(await _outboundService.GetAsync(id, cancellationToken));

    /// <summary>创建出库单（草稿）</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.OutboundEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveOutboundRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _outboundService.CreateAsync(request, cancellationToken), "Outbound order created successfully");

    /// <summary>更新出库单（仅草稿）</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.OutboundEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveOutboundRequest request, CancellationToken cancellationToken)
    {
        await _outboundService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Outbound order updated successfully");
    }

    /// <summary>确认出库单并锁定库存</summary>
    [HttpPost("{id:long}/confirm")]
    [HasPermission(PermissionCodes.OutboundEdit)]
    public async Task<ApiResult<object>> Confirm(long id, CancellationToken cancellationToken)
    {
        await _outboundService.ConfirmAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Outbound order confirmed; stock locked");
    }

    /// <summary>执行出库发运（扣减库存并生成流水）</summary>
    [HttpPost("{id:long}/complete")]
    [HasPermission(PermissionCodes.OutboundExecute)]
    public async Task<ApiResult<object>> Complete(long id, CancellationToken cancellationToken)
    {
        await _outboundService.CompleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Outbound completed; stock deducted");
    }

    /// <summary>取消出库单（已确认状态会释放锁定库存）</summary>
    [HttpPost("{id:long}/cancel")]
    [HasPermission(PermissionCodes.OutboundEdit)]
    public async Task<ApiResult<object>> Cancel(long id, CancellationToken cancellationToken)
    {
        await _outboundService.CancelAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Outbound order cancelled");
    }

    /// <summary>删除出库单（仅草稿）</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.OutboundEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _outboundService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Outbound order deleted");
    }
}
