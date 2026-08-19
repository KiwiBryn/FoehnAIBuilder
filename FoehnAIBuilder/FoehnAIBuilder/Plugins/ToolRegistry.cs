// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;
using MistralAI.Client.DTOs.Shared;

namespace FoehnAIBuilder.Plugins;

/// <summary>
/// Default <see cref="IToolRegistry"/> that loads plugins once, at construction, via
/// <see cref="IToolPluginLoader"/>.
/// </summary>
public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _toolsByName;
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(IToolPluginLoader pluginLoader, ILogger<ToolRegistry> logger)
    {
        _logger = logger;
        var loaded = pluginLoader.LoadPlugins();
        _toolsByName = loaded.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        _logger.LogInformation("Tool registry ready with {Count} tool(s): {ToolNames}",
            _toolsByName.Count, string.Join(", ", _toolsByName.Keys));
    }

    public IReadOnlyList<ITool> Tools => _toolsByName.Values.ToList();

    public List<Tool> BuildToolDefinitions()
    {
        return _toolsByName.Values.Select(tool => new Tool
        {
            Type = "function",
            Function = new FunctionDefinition
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = JsonSerializer.Deserialize<JsonElement>(tool.Command),
            },
        }).ToList();
    }

    public async Task<ToolExecutionResult> ExecuteAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!_toolsByName.TryGetValue(toolName, out var tool))
        {
            _logger.LogWarning("Unknown tool requested by the LLM: {ToolName}", toolName);
            return ToolExecutionResult.Fail($"Unknown tool: '{toolName}'.");
        }

        _logger.LogInformation("Executing tool {ToolName} with arguments {Arguments}", toolName, argumentsJson);

        try
        {
            return await tool.ExecuteAsync(argumentsJson, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {ToolName} threw an unhandled exception", toolName);
            return ToolExecutionResult.Fail($"Tool '{toolName}' failed unexpectedly: {ex.Message}");
        }
    }
}
