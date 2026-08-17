using System.Reflection;
using System.Runtime.Loader;
using FoehnAIBuilder.Abstractions;
using FoehnAIBuilder.Plugins;
using FoehnAIBuilder.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoehnAIBuilder.Plugins;

/// <summary>
/// Loads <see cref="ITool"/> plugins from a flat directory of DLLs at application startup.
/// Plugins are loaded into the default <see cref="AssemblyLoadContext"/> so their shared
/// references (FoehnAIBuilder.Abstractions, Microsoft.Extensions.Logging.Abstractions) resolve
/// to the copies already loaded by this host, rather than duplicate copies - which would
/// otherwise make `is ITool` checks fail across assembly boundaries.
/// </summary>
public sealed class ToolPluginLoader : IToolPluginLoader
{
    private readonly FoehnAIBuilderOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ToolPluginLoader> _logger;

    public ToolPluginLoader(
        IOptions<FoehnAIBuilderOptions> options,
        IServiceProvider serviceProvider,
        ILogger<ToolPluginLoader> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IReadOnlyList<ITool> LoadPlugins()
    {
        var pluginsDirectory = Path.IsPathRooted(_options.PluginsPath)
            ? _options.PluginsPath
            : Path.Combine(AppContext.BaseDirectory, _options.PluginsPath);

        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory not found: {PluginsDirectory}. No tools loaded.", pluginsDirectory);
            return Array.Empty<ITool>();
        }

        var tools = new List<ITool>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dllPath in Directory.EnumerateFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            Assembly assembly;
            try
            {
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
            {
                _logger.LogWarning(ex, "Failed to load plugin assembly {DllPath}", dllPath);
                continue;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
                _logger.LogWarning(ex, "Some types in plugin assembly {AssemblyName} could not be loaded", assembly.GetName().Name);
            }

            var toolTypes = types.Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ITool).IsAssignableFrom(t));

            foreach (var type in toolTypes)
            {
                ITool tool;
                try
                {
                    tool = (ITool)ActivatorUtilities.CreateInstance(_serviceProvider, type);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to instantiate tool plugin {TypeName} from {DllPath}", type.FullName, dllPath);
                    continue;
                }

                if (!seenNames.Add(tool.Name))
                {
                    _logger.LogWarning("Skipping tool {ToolName} from {DllPath}: another plugin already registered that name.", tool.Name, dllPath);
                    continue;
                }

                _logger.LogInformation("Loaded tool '{ToolName}' from {DllPath}", tool.Name, Path.GetFileName(dllPath));
                tools.Add(tool);
            }
        }

        return tools;
    }
}
