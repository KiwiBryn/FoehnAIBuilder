using FoehnAIBuilder.Abstractions;
using Microsoft.Extensions.Logging;

namespace FoehnAI.Tools.Mkdir;

/// <summary>
/// Creates a directory, including any missing parent directories.
/// </summary>
public sealed class MkdirTool : ITool
{
    private readonly ILogger<MkdirTool> _logger;

    public MkdirTool(ILogger<MkdirTool> logger)
    {
        _logger = logger;
    }

    public string Name => "mkdir";

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

    public Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, MkdirJsonContext.Default.MkdirArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse mkdir arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return Task.FromResult(ToolExecutionResult.Fail(jsonError!));
        }

        var path = args.Path;
        if (!ToolPath.TryResolve(path, out var fullPath, out var pathError))
            return Task.FromResult(ToolExecutionResult.Fail(pathError!));

        _logger.LogInformation("Creating directory {Path}", path);

        try
        {
            if (Directory.Exists(fullPath))
                return Task.FromResult(ToolExecutionResult.Ok($"Directory already exists: \"{path}\"."));

            Directory.CreateDirectory(fullPath);
            return Task.FromResult(ToolExecutionResult.Ok($"Created directory \"{path}\"."));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogError(ex, "Error creating directory {Path}", path);
            return Task.FromResult(ToolExecutionResult.Fail($"Error creating \"{path}\": {ex.Message}"));
        }
    }
}
