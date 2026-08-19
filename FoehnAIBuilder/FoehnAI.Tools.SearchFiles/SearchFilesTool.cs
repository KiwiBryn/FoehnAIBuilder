// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.SearchFiles;

/// <summary>
/// Searches file contents under a directory tree for a text substring, so the LLM can
/// locate matches without reading whole files blindly. Complements <c>scan</c>, which
/// only lists file/directory names.
/// </summary>
public sealed class SearchFilesTool : ITool
{
    private const int MaxFilesScanned = 2000;
    private const int MaxMatches = 500;

    private readonly ILogger<SearchFilesTool> _logger;

    public SearchFilesTool(ILogger<SearchFilesTool> logger)
    {
        _logger = logger;
    }

    public string Name => "search_files";

    public string Description =>
        "Searches the contents of files under a given directory tree for a text substring, " +
        "returning the file path and line number of each match. Use this to locate text " +
        "inside files without reading them in full; use 'scan' instead to just list what " +
        "files exist.";

    public string Command => """
        {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "Directory to search. Defaults to the application's current working folder if omitted." },
            "text": { "type": "string", "description": "The text substring to search for within file contents." },
            "pattern": { "type": "string", "description": "File search pattern, e.g. '*.cs'. Defaults to '*' (all files)." },
            "caseSensitive": { "type": "boolean", "description": "Whether the search is case-sensitive. Defaults to false." },
            "recursive": { "type": "boolean", "description": "Whether to recurse into subdirectories. Defaults to true." }
          },
          "required": ["text"]
        }
        """;

    public ToolRiskLevel RiskLevel => ToolRiskLevel.ReadOnly;

    public async Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!ToolArguments.TryParse(argumentsJson, SearchFilesJsonContext.Default.SearchFilesArguments, out var args, out var jsonError))
        {
            _logger.LogWarning("Failed to parse search_files arguments: {Arguments} ({Error})", argumentsJson, jsonError);
            return ToolExecutionResult.Fail(jsonError!);
        }

        if (string.IsNullOrEmpty(args.Text))
            return ToolExecutionResult.Fail("The 'text' argument is required.");

        var sandboxRoot = Directory.GetCurrentDirectory();
        var path = string.IsNullOrWhiteSpace(args.Path) ? sandboxRoot : args.Path;
        var pattern = string.IsNullOrWhiteSpace(args.Pattern) ? "*" : args.Pattern;
        var caseSensitive = args.CaseSensitive ?? false;
        var recursive = args.Recursive ?? true;
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        if (!ToolPath.TryResolve(sandboxRoot, path, out var fullPath, out var pathError))
            return ToolExecutionResult.Fail(pathError!);

        _logger.LogInformation(
            "Searching {Path} for {Text} (pattern={Pattern}, recursive={Recursive}, caseSensitive={CaseSensitive})",
            path, args.Text, pattern, recursive, caseSensitive);

        if (!Directory.Exists(fullPath))
            return ToolExecutionResult.Fail($"Directory not found: {path}");

        List<string> files;
        try
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files = Directory.EnumerateFiles(fullPath, pattern, searchOption).Take(MaxFilesScanned).ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _logger.LogError(ex, "Error enumerating files under {Path}", path);
            return ToolExecutionResult.Fail($"Error searching \"{path}\": {ex.Message}");
        }

        var matches = new List<(string RelativePath, int LineNumber, string Line)>();
        int filesScanned = 0;
        bool matchesTruncated = false;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (matches.Count >= MaxMatches)
            {
                matchesTruncated = true;
                break;
            }

            try
            {
                using var reader = new StreamReader(file);
                filesScanned++;

                int lineNumber = 0;
                string? line;
                while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
                {
                    lineNumber++;

                    if (lineNumber == 1 && line.Contains('\0'))
                        break;

                    if (line.Contains(args.Text, comparison))
                    {
                        matches.Add((Path.GetRelativePath(fullPath, file), lineNumber, line.Trim()));
                        if (matches.Count >= MaxMatches)
                        {
                            matchesTruncated = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                _logger.LogWarning(ex, "Skipping file {File} during search_files", file);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine(
            $"Found {matches.Count} match(es) for \"{args.Text}\" across {filesScanned} file(s) under \"{path}\" " +
            $"(pattern \"{pattern}\", recursive={recursive}, caseSensitive={caseSensitive})" +
            $"{(matchesTruncated ? $" - truncated at {MaxMatches} matches" : "")}.");

        foreach (var (relativePath, lineNumber, line) in matches)
            sb.AppendLine($"{relativePath}:{lineNumber}: {line}");

        return ToolExecutionResult.Ok(sb.ToString());
    }
}
