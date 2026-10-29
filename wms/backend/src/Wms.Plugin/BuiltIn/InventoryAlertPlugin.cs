using Microsoft.Extensions.Logging;

namespace Wms.Plugin.BuiltIn;

/// <summary>库存预警插件输入项</summary>
public class InventoryAlertItem
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal OnHandQty { get; set; }
    public decimal SafetyStock { get; set; }
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// 库存预警插件：对低于安全库存的物料进行汇总，供首页与通知渠道使用。
/// </summary>
public class InventoryAlertPlugin : IWmsPlugin
{
    public string Id => "inventory-alert";
    public string Name => "库存安全预警";
    public string Description => "扫描当前库存，输出低于安全库存的物料清单，可对接站内提醒或第三方通知。";
    public string Category => "库存增强";
    public string Version => "1.0.0";
    public string Author => "WMS Team";

    public Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        if (context.Payload is not IEnumerable<InventoryAlertItem> items)
            return Task.FromResult(PluginExecutionResult.Fail("载荷格式不正确，期望 InventoryAlertItem 集合"));

        var alerts = items
            .Where(x => x.OnHandQty < x.SafetyStock)
            .OrderBy(x => x.OnHandQty - x.SafetyStock)
            .Select(x => new
            {
                x.MaterialCode,
                x.MaterialName,
                x.WarehouseName,
                x.OnHandQty,
                x.SafetyStock,
                x.Unit,
                ShortageQty = x.SafetyStock - x.OnHandQty
            })
            .ToList();

        context.Logger.LogInformation("库存预警插件执行完成，发现 {Count} 条预警", alerts.Count);

        return Task.FromResult(PluginExecutionResult.Ok(
            alerts.Count == 0 ? "所有物料库存均高于安全库存" : $"发现 {alerts.Count} 条库存预警",
            alerts));
    }
}
