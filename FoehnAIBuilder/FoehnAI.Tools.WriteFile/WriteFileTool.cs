// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.WriteFile;

/// <summary>
/// Writes text content to a file, creating the file (and any missing parent
/// directories) if it doesn't already exist.
/// </summary>
public sealed class WriteFileTool(ILogger<WriteFileTool> logger) : ITool
{
   public string Name => "file.write";

    public string Description =>
        "Writes text content to a file at the given path, creating the file (and any missing " +
        "parent directories) if it doesn't already exist.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path to the file to write." },
            "content": { "type": "string", "description": "Text content to write to the file." },
            "overwrite": { "type": "boolean", "description": "Whether to overwrite the file if it already exists. Defaults to true." }
          },
          "required": ["path", "content"]
        }
        """;

   public ToolRiskLevel RiskLevel => ToolRiskLevel.Write;

    public async Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, WriteFileJsonContext.Default.WriteFileArguments, out var args, out var jsonError))
        {
            logger.LogWarning("Failed to parse file.write arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return ToolExecutionResult.Fail(jsonError!);
        }

      var path = args.Path;
      var content = args.Content;
      var overwrite = args.Overwrite ?? true;

      if (!ToolPath.TryResolve(path, out var fullPath, out var pathError))
         return ToolExecutionResult.Fail(pathError!);

      if (!overwrite && File.Exists(fullPath))
         return ToolExecutionResult.Fail($"File already exists and overwrite is false: {path}");

        logger.LogInformation("Writing {Length} characters to {Path}", content.Length, path);

      try
      {
         var directory = Path.GetDirectoryName(fullPath);
         if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
            return ToolExecutionResult.Ok($"Wrote {content.Length} characters to \"{path}\".");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            logger.LogError(ex, "Error writing file {Path}", path);
            return ToolExecutionResult.Fail($"Error writing \"{path}\": {ex.Message}");
        }
    }
}
