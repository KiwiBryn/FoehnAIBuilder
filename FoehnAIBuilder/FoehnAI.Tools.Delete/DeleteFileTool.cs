// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.Delete;

/// <summary>
/// Deletes a single file.
/// </summary>
public sealed class DeleteFileTool(ILogger<DeleteFileTool> logger) : ITool
{
   public string Name => "file.delete";

    public string Description => "Deletes a single file at the given path.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path to the file to delete (relative or absolute)." }
          },
          "required": ["path"]
        }
        """;

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Destructive;

    public async Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, DeleteJsonContext.Default.DeleteArguments, out var args, out var jsonError))
        {
            logger.LogWarning("Failed to parse delete arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return ToolExecutionResult.Fail(jsonError!);
        }

        var path = args.Path;
        if (!ToolPath.TryResolve(path, out var fullPath, out var pathError))
            return ToolExecutionResult.Fail(pathError!);

        logger.LogInformation("Deleting file {Path}", path);

        if (!File.Exists(fullPath))
            return ToolExecutionResult.Fail($"File not found: {path}");

        try
        {
            File.Delete(fullPath);
            return ToolExecutionResult.Ok($"Deleted \"{path}\".");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            logger.LogError(ex, "Error deleting file {Path}", path);

            return ToolExecutionResult.Fail($"Error deleting \"{path}\": {ex.Message}");
        }
    }
}
