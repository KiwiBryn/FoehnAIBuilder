using FoehnAIBuilder.Abstractions;
using FoehnAI.Tools.ReadFile;
using Microsoft.Extensions.Logging;

namespace FoehnAIBuilder.Tools.ReadFile;

/// <summary>
/// Reads and returns the full text contents of a file.
/// </summary>
public sealed class ReadFileTool : ITool
{
    private const int MaxCharacters = 200_000;

    private readonly ILogger<ReadFileTool> _logger;

    public ReadFileTool(ILogger<ReadFileTool> logger)
    {
        _logger = logger;
    }

    public string Name => "read_file";

    public string Description => "Reads and returns the full text contents of a file at the given path.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Path to the file to read (relative or absolute)." }
          },
          "required": ["path"]
        }
        """;

    public ToolRiskLevel RiskLevel => ToolRiskLevel.ReadOnly;

    public async Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, ReadFileJsonContext.Default.ReadFileArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse read_file arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return ToolExecutionResult.Fail(jsonError!);
        }

        var path = args.Path;
        if (!ToolPath.TryResolve(path, out var fullPath, out var pathError))
            return ToolExecutionResult.Fail(pathError!);

        _logger.LogInformation("Reading file {Path}", path);

        if (!File.Exists(fullPath))
            return ToolExecutionResult.Fail($"File not found: {path}");

        try
        {
            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
            if (content.Length > MaxCharacters)
            {
                var truncated = content[..MaxCharacters];
                return ToolExecutionResult.Ok(
                    $"{truncated}\n\n[Output truncated at {MaxCharacters} characters; file is {content.Length} characters long.]");
            }

            return ToolExecutionResult.Ok(content);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogError(ex, "Error reading file {Path}", path);
            return ToolExecutionResult.Fail($"Error reading \"{path}\": {ex.Message}");
        }
    }
}
