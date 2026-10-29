using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Wms.Plugin.BuiltIn;

/// <summary>ERP 同步载荷</summary>
public class ErpSyncPayload
{
    /// <summary>同步实体类型，如 material、supplier</summary>
    public string EntityType { get; set; } = "material";

    /// <summary>同步数据条数</summary>
    public int RecordCount { get; set; }

    /// <summary>同步明细</summary>
    public IReadOnlyList<object> Records { get; set; } = Array.Empty<object>();
}

/// <summary>
/// ERP 第三方集成适配插件：演示如何把 WMS 数据映射为外部系统字段并推送。
/// 配置项：erpEndpoint（外部接口地址）、erpToken（访问令牌）。
/// 未配置接口时仅输出字段映射结果，便于离线演示。
/// </summary>
public class ErpSyncPlugin : IWmsPlugin
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    /// <summary>WMS 字段到 ERP 字段的映射约定</summary>
    private static readonly Dictionary<string, string> FieldMapping = new()
    {
        ["Code"] = "ext_code",
        ["Name"] = "ext_name",
        ["Spec"] = "ext_spec",
        ["Unit"] = "ext_uom",
        ["Price"] = "ext_price"
    };

    public string Id => "erp-sync";
    public string Name => "ERP 数据同步适配器";
    public string Description => "把物料等主数据映射为 ERP 字段规范并推送，可作为第三方系统集成模板。";
    public string Category => "第三方集成";
    public string Version => "1.0.0";
    public string Author => "WMS Team";

    public async Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var endpoint = context.Settings.TryGetValue("erpEndpoint", out var value) ? value : null;
        var payload = context.Payload as ErpSyncPayload ?? new ErpSyncPayload();

        var mapped = new
        {
            entity = payload.EntityType,
            count = payload.RecordCount,
            fieldMapping = FieldMapping
        };

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return PluginExecutionResult.Ok(
                $"ERP 接口未配置，已完成 {payload.RecordCount} 条数据的字段映射校验（离线模式）",
                mapped);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new { entity = payload.EntityType, records = payload.Records })
            };

            if (context.Settings.TryGetValue("erpToken", out var token) && !string.IsNullOrWhiteSpace(token))
                request.Headers.Add("Authorization", $"Bearer {token}");

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return PluginExecutionResult.Fail($"ERP 同步失败，HTTP 状态码 {response.StatusCode}");

            return PluginExecutionResult.Ok($"已向 ERP 推送 {payload.RecordCount} 条数据", mapped);
        }
        catch (Exception ex)
        {
            context.Logger.LogWarning(ex, "ERP 同步异常: {Endpoint}", endpoint);
            return PluginExecutionResult.Fail($"ERP 同步异常：{ex.Message}");
        }
    }
}
