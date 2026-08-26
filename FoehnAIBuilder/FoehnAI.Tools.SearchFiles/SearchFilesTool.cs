// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.SearchFiles;

/// <summary>
/// Searches file contents under a directory tree for a text substring, so the LLM can
/// locate matches without reading whole files blindly. Complements <c>scan</c>, which
/// only lists file/directory names.
/// </summary>
public sealed class SearchFilesTool(ILogger<SearchFilesTool> logger) : ITool
{
   private const int MaxFilesScanned = 2000;
   private const int MaxMatches = 500;

   public string Name => "files.search";

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
         logger.LogWarning("Failed to parse search_files arguments: {Arguments} ({Error})", argumentsJson, jsonError);
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

      logger.LogInformation(
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
         logger.LogError(ex, "Error enumerating files under {Path}", path);
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
            logger.LogWarning(ex, "Skipping file {File} during search_files", file);
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


/*
// Copyright (c) August 2026, devMobile Software
// 
using System.Collections.Concurrent;
using FoehnAIBuilder.Abstractions;

namespace FoehnAI.Tools.SearchFiles;

/// <summary>
/// Searches file contents under a directory tree for a text substring, so the LLM can
/// locate matches without reading whole files blindly. Complements <c>scan</c>, which
/// only lists file/directory names.
/// </summary>
public sealed class SearchFilesTool(ILogger<SearchFilesTool> logger) : ITool
{
    private const int MaxFilesScanned = 2000;
    private const int MaxMatches = 500;
    private const long MaxFileSizeBytes = 50L * 1024 * 1024; // 50 MB per-file cap
    private const int MaxLineLength = 8192;                  // skip absurdly long lines
    private const int MaxSnippetLength = 500;                // truncate stored match text
    private const int ReadBufferSize = 64 * 1024;            // 64 KB stream buffer
    private const int BinarySniffBytes = 4096;               // sniff first 4 KB for NUL bytes

    // Extensions that are almost certainly binary — skip without opening.
    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll", ".exe", ".pdb", ".so", ".dylib", ".a", ".lib", ".obj", ".o",
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".tif", ".tiff",
        ".zip", ".7z", ".gz", ".tar", ".rar", ".bz2", ".xz",
        ".mp3", ".mp4", ".mov", ".avi", ".wav", ".flac", ".mkv",
        ".pdf", ".class", ".jar", ".nupkg", ".bin", ".dat", ".wasm"
    };

    public string Name => "files.search";

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
            logger.LogWarning("Failed to parse search_files arguments: {Arguments} ({Error})", argumentsJson, jsonError);
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
        var text = args.Text;

        if (!ToolPath.TryResolve(sandboxRoot, path, out var fullPath, out var pathError))
            return ToolExecutionResult.Fail(pathError!);

        logger.LogInformation(
            "Searching {Path} for {Text} (pattern={Pattern}, recursive={Recursive}, caseSensitive={CaseSensitive})",
            path, text, pattern, recursive, caseSensitive);

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
            logger.LogError(ex, "Error enumerating files under {Path}", path);
            return ToolExecutionResult.Fail($"Error searching \"{path}\": {ex.Message}");
        }

        var matches = new ConcurrentBag<(string RelativePath, int LineNumber, string Line)>();
        int filesScanned = 0;
        int filesSkipped = 0;
        int totalMatches = 0;
        int matchesTruncatedFlag = 0;

        try
        {
            await Parallel.ForEachAsync(
                files,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                },
                async (file, ct) =>
                {
                    if (Volatile.Read(ref matchesTruncatedFlag) != 0)
                        return;

                    // Cheap up-front skip: known-binary extensions.
                    if (BinaryExtensions.Contains(Path.GetExtension(file)))
                    {
                        Interlocked.Increment(ref filesSkipped);
                        return;
                    }

                    try
                    {
                        var fileMatches = await ScanFileAsync(file, text, comparison, MaxMatches, ct);
                        Interlocked.Increment(ref filesScanned);

                        if (fileMatches.Count == 0)
                            return;

                        var relative = Path.GetRelativePath(fullPath, file);
                        foreach (var (lineNumber, line) in fileMatches)
                        {
                            var newTotal = Interlocked.Increment(ref totalMatches);
                            if (newTotal > MaxMatches)
                            {
                                Interlocked.Exchange(ref matchesTruncatedFlag, 1);
                                return;
                            }
                            matches.Add((relative, lineNumber, line));
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        Interlocked.Increment(ref filesSkipped);
                        logger.LogWarning(ex, "Skipping file {File} during search_files", file);
                    }
                });
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        var matchesTruncated = matchesTruncatedFlag != 0;

        // Stable output order.
        var ordered = matches
            .OrderBy(m => m.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.LineNumber)
            .Take(MaxMatches)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("Found ").Append(ordered.Count).Append(" match(es) for \"").Append(text)
          .Append("\" across ").Append(filesScanned).Append(" file(s)");
        if (filesSkipped > 0)
            sb.Append(" (").Append(filesSkipped).Append(" skipped)");
        sb.Append(" under \"").Append(path).Append("\" (pattern \"").Append(pattern)
          .Append("\", recursive=").Append(recursive)
          .Append(", caseSensitive=").Append(caseSensitive).Append(')');
        if (matchesTruncated)
            sb.Append(" - truncated at ").Append(MaxMatches).Append(" matches");
        sb.Append('.').Append('\n');

        foreach (var (relativePath, lineNumber, line) in ordered)
            sb.Append(relativePath).Append(':').Append(lineNumber).Append(": ").Append(line).Append('\n');

        return ToolExecutionResult.Ok(sb.ToString());
    }

    private static async Task<List<(int LineNumber, string Line)>> ScanFileAsync(
        string file, string text, StringComparison comparison, int maxMatches, CancellationToken ct)
    {
        var results = new List<(int, string)>();

        var info = new FileInfo(file);
        if (info.Length == 0 || info.Length > MaxFileSizeBytes)
            return results;

        await using var stream = new FileStream(
            file,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: ReadBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        // Cheap binary sniff: any NUL byte in the first few KB means "binary".
        var sniffLen = (int)Math.Min(BinarySniffBytes, info.Length);
        var sniffBuffer = new byte[sniffLen];
        var read = await stream.ReadAsync(sniffBuffer.AsMemory(0, sniffLen), ct);
        if (sniffBuffer.AsSpan(0, read).IndexOf((byte)0) >= 0)
            return results;

        stream.Position = 0;

        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        int lineNumber = 0;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineNumber++;

            if (line.Length > MaxLineLength)
                continue;

            if (line.Contains(text, comparison))
            {
                var snippet = line.Length > MaxSnippetLength
                    ? string.Concat(line.AsSpan(0, MaxSnippetLength), "…")
                    : line;
                results.Add((lineNumber, snippet.Trim()));

                if (results.Count >= maxMatches)
                    break;
            }
        }

        return results;
    }

/*
## Key changes vs. original

1. **Real async I/O** — `FileOptions.Asynchronous | FileOptions.SequentialScan` with a 64 KB buffer.
2. **Parallel scan** — `Parallel.ForEachAsync` bounded by `Environment.ProcessorCount`.
3. **Per-file size cap** — 50 MB; skips oversized files fast via `FileInfo.Length`.
4. **Better binary detection** — extension allow-list *plus* 4 KB NUL-byte sniff (not just line 1).
5. **Max line length** — skips lines > 8 KB to avoid pathological minified files.
6. **Snippet truncation** — matches over 500 chars are truncated with `…`.
7. **`FileShare.ReadWrite | FileShare.Delete`** — can scan files open for writing.
8. **Stable output** — results sorted by path + line number after parallel collection.
9. **Skipped-file count** reported in the summary.
10. **`'\n'` instead of `Environment.NewLine`** for portable LLM output.
11. **Thread-safe counters** via `Interlocked` and `ConcurrentBag`.

## Notes / tunables to consider
- If your `ITool` framework guarantees single-threaded execution, the concurrency is fine. If tools can already run in parallel, you may want to lower `MaxDegreeOfParallelism`.
- `MaxFileSizeBytes`, `MaxLineLength`, `MaxSnippetLength` are compile-time constants — promote to `Command` schema options if callers need control.
- The binary extension list is conservative; add project-specific ones (`.snupkg`, `.suo`, `.user`, etc.) as needed.

*/