using System.Text.Json;
using Wms.Core.Common;

namespace Wms.WebApi.Middleware;

/// <summary>
/// 全局异常处理中间件：将业务异常与未处理异常转换为统一的 API 响应结构。
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            _logger.LogWarning("Business exception [{Code}]: {Message}, path {Path}", ex.Code, ex.Message, context.Request.Path);
            await WriteAsync(context, MapStatusCode(ex.Code), ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception, path {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, ErrorCodes.ServerError, "Internal server error. Please try again later");
        }
    }

    private static int MapStatusCode(int code) => code switch
    {
        ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
        ErrorCodes.NotFound => StatusCodes.Status404NotFound,
        ErrorCodes.Conflict => StatusCodes.Status409Conflict,
        ErrorCodes.ServerError => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest
    };

    private static async Task WriteAsync(HttpContext context, int statusCode, int code, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = ApiResult<object>.Fail(code, message, context.TraceIdentifier);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
