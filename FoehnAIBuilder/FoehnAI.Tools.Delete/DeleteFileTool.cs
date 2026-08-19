// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.Delete;

/// <summary>
/// Deletes a single file.
/// </summary>
public sealed class DeleteFileTool : ITool
{
    private readonly ILogger<DeleteFileTool> _logger;

    public DeleteFileTool(ILogger<DeleteFileTool> logger)
    {
        _logger = logger;
    }

    public string Name => "delete";

    public string Description => "Deletes a single file at the given path.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path to the file to delete." }
          },
          "required": ["path"]
        }
        """;

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Destructive;

    public Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, DeleteJsonContext.Default.DeleteArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse delete arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return Task.FromResult(ToolExecutionResult.Fail(jsonError!));
        }

        var path = args.Path;
        if (!ToolPath.TryResolve(path, out var fullPath, out var pathError))
            return Task.FromResult(ToolExecutionResult.Fail(pathError!));

        _logger.LogInformation("Deleting file {Path}", path);

        if (!File.Exists(fullPath))
            return Task.FromResult(ToolExecutionResult.Fail($"File not found: {path}"));

        try
        {
            File.Delete(fullPath);
            return Task.FromResult(ToolExecutionResult.Ok($"Deleted \"{path}\"."));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogError(ex, "Error deleting file {Path}", path);
            return Task.FromResult(ToolExecutionResult.Fail($"Error deleting \"{path}\": {ex.Message}"));
        }
    }
}
