using FoehnAIBuilder.Abstractions;
using Microsoft.Extensions.Logging;

namespace FoehnAI.Tools.Rename;

/// <summary>
/// Renames or moves a file from one path to another.
/// </summary>
public sealed class RenameTool : ITool
{
    private readonly ILogger<RenameTool> _logger;

    public RenameTool(ILogger<RenameTool> logger)
    {
        _logger = logger;
    }

    public string Name => "rename";

    public string Description => "Renames or moves a file from one path to another.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "sourcePath": { "type": "string", "description": "The current path of the file." },
            "destinationPath": { "type": "string", "description": "The new path/name for the file." }
          },
          "required": ["sourcePath", "destinationPath"]
        }
        """;

    public ToolRiskLevel RiskLevel => ToolRiskLevel.Write;

    public Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, RenameJsonContext.Default.RenameArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse rename arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return Task.FromResult(ToolExecutionResult.Fail(jsonError!));
        }

        var sourcePath = args.SourcePath;
        var destinationPath = args.DestinationPath;

        if (!ToolPath.TryResolve(sourcePath, out var sourceFullPath, out var sourcePathError))
            return Task.FromResult(ToolExecutionResult.Fail(sourcePathError!));
        if (!ToolPath.TryResolve(destinationPath, out var destinationFullPath, out var destinationPathError))
            return Task.FromResult(ToolExecutionResult.Fail(destinationPathError!));

        _logger.LogInformation("Renaming {SourcePath} to {DestinationPath}", sourcePath, destinationPath);

        if (!File.Exists(sourceFullPath))
            return Task.FromResult(ToolExecutionResult.Fail($"Source file not found: {sourcePath}"));

        if (File.Exists(destinationFullPath))
            return Task.FromResult(ToolExecutionResult.Fail($"Destination file already exists: {destinationPath}"));

        try
        {
            var destDirectory = Path.GetDirectoryName(destinationFullPath);
            if (!string.IsNullOrEmpty(destDirectory))
                Directory.CreateDirectory(destDirectory);

            File.Move(sourceFullPath, destinationFullPath);
            return Task.FromResult(ToolExecutionResult.Ok($"Renamed \"{sourcePath}\" to \"{destinationPath}\"."));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogError(ex, "Error renaming {SourcePath} to {DestinationPath}", sourcePath, destinationPath);
            return Task.FromResult(ToolExecutionResult.Fail($"Error renaming \"{sourcePath}\" to \"{destinationPath}\": {ex.Message}"));
        }
    }
}
