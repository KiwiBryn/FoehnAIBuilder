// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.Rmdir;

/// <summary>
/// Removes a directory, optionally including everything inside it.
/// </summary>
public sealed class RmdirTool : ITool
{
    private readonly ILogger<RmdirTool> _logger;

    public RmdirTool(ILogger<RmdirTool> logger)
    {
        _logger = logger;
    }

    public string Name => "rmdir";

    public string Description => "Removes a directory at the given path.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path of the directory to remove." },
            "recursive": { "type": "boolean", "description": "Whether to delete the directory even if it contains files/subdirectories. Defaults to false." }
          },
          "required": ["path"]
        }
        """;

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Destructive;

    public Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, RmdirJsonContext.Default.RmdirArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse rmdir arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return Task.FromResult(ToolExecutionResult.Fail(jsonError!));
        }

        var path = args.Path;
        var recursive = args.Recursive ?? false;

        if (!ToolPath.TryResolve(path, out var fullPath, out var pathError))
            return Task.FromResult(ToolExecutionResult.Fail(pathError!));

        _logger.LogInformation("Removing directory {Path} (recursive={Recursive})", path, recursive);

        if (!Directory.Exists(fullPath))
            return Task.FromResult(ToolExecutionResult.Fail($"Directory not found: {path}"));

        try
        {
            Directory.Delete(fullPath, recursive);
            return Task.FromResult(ToolExecutionResult.Ok($"Removed directory \"{path}\"."));
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Directory not empty: {Path}", path);
            return Task.FromResult(ToolExecutionResult.Fail(
                $"Could not remove \"{path}\": {ex.Message} (pass recursive=true to delete a non-empty directory)."));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Error removing directory {Path}", path);
            return Task.FromResult(ToolExecutionResult.Fail($"Error removing \"{path}\": {ex.Message}"));
        }
    }
}
