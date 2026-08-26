using System;
using System.Linq;
using System.Text;
using FoehnAIBuilder.Abstractions;
using FoehnSharp.Tools.Scan;
using Microsoft.Extensions.Logging;

namespace FoehnAIBuilder.Tools.Scan;

/// <summary>
/// Recursively (by default) lists files under a directory tree, so the LLM can see
/// what exists before reading, writing, or executing anything.
/// </summary>
public sealed class ScanTool : ITool
{
    private const int MaxEntries = 2000;

    private readonly ILogger<ScanTool> _logger;

    public ScanTool(ILogger<ScanTool> logger)
    {
        _logger = logger;
    }

    public string Name => "scan";

    public string Description =>
        "Recursively lists files and directories under a given path, or the current working " +
        "folder if no path is supplied. Use this first to discover what exists before reading, " +
        "writing, deleting, or executing anything.";

    public string Command => $$"""
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Directory to scan. Defaults to the application's current working folder if omitted." },
            "pattern": { "type": "string", "description": "Search pattern, e.g. '*.cs'. Defaults to '*' (all files)." },
            "recursive": { "type": "boolean", "description": "Whether to recurse into subdirectories. Defaults to true." },
            "ignoreFiles": { "type": "array", "items": { "type": "string" }, "description": "List of file names to ignore (e.g., [\"temp.txt\", \"backup.dat\"])." },
            "ignoreExtensions": { "type": "array", "items": { "type": "string" }, "description": "List of file extensions to ignore (e.g., [\".log\", \".tmp\"])." },
            "ignoreFolders": { "type": "array", "items": { "type": "string" }, "description": "List of folder names to ignore (e.g., [\"node_modules\", \"bin\"])." }
          },
          "required": []
        }
        """;

    public ToolRiskLevel RiskLevel => ToolRiskLevel.ReadOnly;

    public Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, ScanJsonContext.Default.ScanArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse scan arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return Task.FromResult(ToolExecutionResult.Fail(jsonError!));
        }

        var sandboxRoot = Directory.GetCurrentDirectory();
        var path = string.IsNullOrWhiteSpace(args.Path) ? sandboxRoot : args.Path;
        var pattern = string.IsNullOrWhiteSpace(args.Pattern) ? "*" : args.Pattern;
        var recursive = args.Recursive ?? true;

        if (!ToolPath.TryResolve(sandboxRoot, path, out var fullPath, out var pathError))
            return Task.FromResult(ToolExecutionResult.Fail(pathError!));

        _logger.LogInformation("Scanning {Path} (pattern={Pattern}, recursive={Recursive})", path, pattern, recursive);

        if (!Directory.Exists(fullPath))
            return Task.FromResult(ToolExecutionResult.Fail($"Directory not found: {path}"));

        try
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.EnumerateFiles(fullPath, pattern, searchOption).Take(MaxEntries + 1).ToList();

            // Apply ignore filters
            if (args.IgnoreFiles != null && args.IgnoreFiles.Length > 0)
            {
                files = files.Where(f => !args.IgnoreFiles.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)).ToList();
            }

            if (args.IgnoreExtensions != null && args.IgnoreExtensions.Length > 0)
            {
                files = files.Where(f => !args.IgnoreExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase)).ToList();
            }

            if (args.IgnoreFolders != null && args.IgnoreFolders.Length > 0 && recursive)
            {
                files = files.Where(f => 
                {
                    var directory = Path.GetDirectoryName(f);
                    if (directory == null) return true;
                    
                    var relativePath = Path.GetRelativePath(fullPath, directory);
                    return !args.IgnoreFolders.Any(ignoreFolder => 
                        relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Contains(ignoreFolder, StringComparer.OrdinalIgnoreCase));
                }).ToList();
            }

            bool truncated = files.Count > MaxEntries;
            if (truncated)
                files = files.Take(MaxEntries).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Found {files.Count} file(s) under \"{path}\" (pattern \"{pattern}\", recursive={recursive}){(truncated ? $" - truncated at {MaxEntries} entries" : "")}.");
            foreach (var file in files)
                sb.AppendLine(Path.GetRelativePath(fullPath, file));

            return Task.FromResult(ToolExecutionResult.Ok(sb.ToString()));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogError(ex, "Error scanning {Path}", path);
            return Task.FromResult(ToolExecutionResult.Fail($"Error scanning \"{path}\": {ex.Message}"));
        }
    }
}
