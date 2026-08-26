// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.Mkdir;

/// <summary>
/// Creates a directory, including any missing parent directories.
/// </summary>
public sealed class MkdirTool(ILogger<MkdirTool> logger) : ITool
{
   public string Name => "directory.create";

   public string Description => "Creates a directory (and any missing parent directories) at the given path.";

   public string Command => """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path of the directory to create." }
          },
          "required": ["path"]
        }
        """;

   public ToolRiskLevel RiskLevel => ToolRiskLevel.Write;

   public async Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
   {
      if (!ToolArguments.TryParse(argumentsJson, MkdirJsonContext.Default.MkdirArguments, out var args, out var jsonError))
      {
         logger.LogWarning("Failed to parse mkdir arguments: {Arguments} ({Error})", argumentsJson, jsonError);
         return ToolExecutionResult.Fail(jsonError!);
      }

      var path = args.Path;
      if (!ToolPath.TryResolve(path, out var fullPath, out var pathError))
         return ToolExecutionResult.Fail(pathError!);

      logger.LogInformation("Creating directory {Path}", path);

      try
      {
         if (Directory.Exists(fullPath))
            return ToolExecutionResult.Ok($"Directory already exists: \"{path}\".");

         Directory.CreateDirectory(fullPath);
         return ToolExecutionResult.Ok($"Created directory \"{path}\".");
      }
      catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
      {
         logger.LogError(ex, "Error creating directory {Path}", path);
         return ToolExecutionResult.Fail($"Error creating \"{path}\": {ex.Message}");
      }
   }
}
