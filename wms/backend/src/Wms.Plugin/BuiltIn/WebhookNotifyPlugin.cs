using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Wms.Plugin.BuiltIn;

/// <summary>
/// Webhook 通知插件：将宿主传入的业务数据以 JSON 形式推送到第三方系统（如企业微信机器人、钉钉机器人）。
/// 配置项：webhookUrl（必填）、secret（可选，预留签名扩展）。
/// </summary>
public class WebhookNotifyPlugin : IWmsPlugin
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public string Id => "webhook-notify";
    public string Name => "Webhook 消息推送";
    public string Description => "把库存预警、作业完成等事件以 JSON 推送到第三方 Webhook 地址。";
    public string Category => "第三方集成";
    public string Version => "1.0.0";
    public string Author => "WMS Team";

    public async Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var url = context.Settings.TryGetValue("webhookUrl", out var value) ? value : null;
        if (string.IsNullOrWhiteSpace(url))
            return PluginExecutionResult.Fail("未配置 webhookUrl，请在插件配置中填写目标地址");

        try
        {
            var response = await HttpClient.PostAsJsonAsync(url, new
            {
                source = "WMS",
                sentAt = DateTime.Now,
                data = context.Payload
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return PluginExecutionResult.Fail($"推送失败，HTTP 状态码 {response.StatusCode}");

            context.Logger.LogInformation("Webhook 推送成功: {Url}", url);
            return PluginExecutionResult.Ok("Webhook 推送成功", new { statusCode = (int)response.StatusCode });
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "Webhook 推送异常: {Url}", url);
            return PluginExecutionResult.Fail($"推送异常：{ex.Message}");
        }
    }
}
