namespace Wms.Core.Common;

/// <summary>
/// 统一 API 响应结构，所有对外接口均返回该结构，便于前端统一处理。
/// </summary>
/// <typeparam name="T">业务数据类型</typeparam>
public class ApiResult<T>
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>业务状态码，0 表示成功</summary>
    public int Code { get; set; }

    /// <summary>提示信息</summary>
    public string Message { get; set; } = "ok";

    /// <summary>业务数据</summary>
    public T? Data { get; set; }

    /// <summary>链路追踪标识，便于问题定位</summary>
    public string? TraceId { get; set; }

    /// <summary>构造成功响应</summary>
    public static ApiResult<T> Ok(T data, string message = "ok")
        => new() { Success = true, Code = 0, Message = message, Data = data };

    /// <summary>构造失败响应</summary>
    public static ApiResult<T> Fail(int code, string message, string? traceId = null)
        => new() { Success = false, Code = code, Message = message, TraceId = traceId };
}
