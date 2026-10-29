using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Wms.Plugin;

/// <summary>
/// 插件管理器：负责内置插件注册、外部 DLL 动态加载与执行调度。
/// </summary>
public class PluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly Dictionary<string, IWmsPlugin> _plugins = new();
    private readonly object _syncRoot = new();

    public PluginManager(ILogger<PluginManager> logger)
    {
        _logger = logger;
        RegisterBuiltIn(new BuiltIn.InventoryAlertPlugin());
        RegisterBuiltIn(new BuiltIn.WebhookNotifyPlugin());
        RegisterBuiltIn(new BuiltIn.ErpSyncPlugin());
    }

    /// <summary>注册内置插件</summary>
    private void RegisterBuiltIn(IWmsPlugin plugin)
    {
        lock (_syncRoot)
        {
            _plugins[plugin.Id] = plugin;
        }
    }

    /// <summary>获取全部已加载插件</summary>
    public IReadOnlyList<IWmsPlugin> GetAll()
    {
        lock (_syncRoot)
        {
            return _plugins.Values.OrderBy(p => p.Id).ToList();
        }
    }

    /// <summary>按插件 ID 查找插件</summary>
    public IWmsPlugin? Get(string id)
    {
        lock (_syncRoot)
        {
            return _plugins.TryGetValue(id, out var plugin) ? plugin : null;
        }
    }

    /// <summary>
    /// 从指定目录加载外部插件 DLL，实现「编译即插拔」的扩展机制。
    /// 每个 DLL 中实现 IWmsPlugin 的类型都会被实例化并注册。
    /// </summary>
    public void LoadExternalPlugins(string directory)
    {
        if (!Directory.Exists(directory))
        {
            _logger.LogInformation("插件目录不存在，跳过外部插件加载: {Directory}", directory);
            return;
        }

        foreach (var file in Directory.GetFiles(directory, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IWmsPlugin).IsAssignableFrom(t) && !t.IsAbstract && t.IsInterface);

                foreach (var type in pluginTypes)
                {
                    if (Activator.CreateInstance(type) is not IWmsPlugin plugin)
                        continue;

                    lock (_syncRoot)
                    {
                        _plugins[plugin.Id] = plugin;
                    }
                    _logger.LogInformation("已加载外部插件 {PluginName} ({PluginId}) 来自 {File}", plugin.Name, plugin.Id, file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "加载外部插件失败: {File}", file);
            }
        }
    }
}
