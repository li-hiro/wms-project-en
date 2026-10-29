using System.Diagnostics;
using Wms.App.Entities;
using Wms.App.Services;
using Wms.Core.Security;

namespace Wms.WebApi.Middleware;

/// <summary>
/// 操作日志中间件：记录所有非 GET 的写操作，用于审计与问题追查。
/// </summary>
public class OperationLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationLogMiddleware> _logger;

    public OperationLogMiddleware(RequestDelegate next, ILogger<OperationLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, OperationLogService logService)
    {
        var method = context.Request.Method;
        var shouldLog = context.Request.Path.StartsWithSegments("/api")
                        && !HttpMethods.IsGet(method)
                        && !HttpMethods.IsHead(method)
                        && !HttpMethods.IsOptions(method);

        if (!shouldLog)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        try
        {
            var userIdValue = context.User?.FindFirst(JwtTokenService.ClaimUserId)?.Value;
            long? userId = long.TryParse(userIdValue, out var parsed) ? parsed : null;

            var log = new OperationLog
            {
                UserId = userId,
                UserName = context.User?.Identity?.Name,
                Module = ResolveModule(context.Request.Path),
                Action = $"{method} {context.Request.Path}",
                Method = method,
                Path = context.Request.Path,
                Ip = context.Connection.RemoteIpAddress?.ToString(),
                Params = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                Success = context.Response.StatusCode < StatusCodes.Status400BadRequest,
                DurationMs = stopwatch.ElapsedMilliseconds
            };

            await logService.WriteAsync(log);
        }
        catch (Exception ex)
        {
            // 日志记录失败不应影响主流程
            _logger.LogWarning(ex, "Failed to write operation log");
        }
    }

    private static string ResolveModule(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var module = segments.Length > 1 ? segments[1] : "api";

        return module switch
        {
            "auth" => "Authentication",
            "users" => "User Management",
            "roles" => "Role Management",
            "menus" => "Menu Management",
            "logs" => "Operation Logs",
            "plugins" => "Plugin Management",
            "material-categories" => "Material Categories",
            "materials" => "Material Management",
            "suppliers" => "Suppliers",
            "customers" => "Customers",
            "warehouses" => "Warehouse Management",
            "locations" => "Location Management",
            "inbounds" => "Inbound Operations",
            "outbounds" => "Outbound Operations",
            "stocktakes" => "Stocktake",
            "transfers" => "Stock Transfers",
            "inventory" => "Inventory Management",
            "reports" => "Reports",
            "dashboard" => "Dashboard",
            _ => module
        };
    }
}
