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
            _logger.LogWarning(ex, "写入操作日志失败");
        }
    }

    private static string ResolveModule(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        var module = segments.Length > 1 ? segments[1] : "api";

        return module switch
        {
            "auth" => "认证",
            "users" => "用户管理",
            "roles" => "角色管理",
            "menus" => "菜单管理",
            "logs" => "操作日志",
            "plugins" => "插件管理",
            "material-categories" => "物料分类",
            "materials" => "物料管理",
            "suppliers" => "供应商",
            "customers" => "客户",
            "warehouses" => "仓库管理",
            "locations" => "库位管理",
            "inbounds" => "入库作业",
            "outbounds" => "出库作业",
            "stocktakes" => "库存盘点",
            "transfers" => "库存调拨",
            "inventory" => "库存管理",
            "reports" => "报表统计",
            "dashboard" => "工作台",
            _ => module
        };
    }
}
