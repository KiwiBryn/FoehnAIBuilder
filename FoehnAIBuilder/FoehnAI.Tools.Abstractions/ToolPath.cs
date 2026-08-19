// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAIBuilder.Abstractions;

/// <summary>
/// Resolves LLM-supplied paths against the tool host's current working directory and
/// rejects anything that resolves outside of it, so a tool argument like "../../secrets"
/// or an absolute path can't escape the sandboxed root.
/// </summary>
public static class ToolPath
{
    /// <summary>
    /// Resolves <paramref name="requestedPath"/> against the process's current working
    /// directory. See the two-argument overload for details.
    /// </summary>
    public static bool TryResolve(string? requestedPath, out string fullPath, out string? error) =>
        TryResolve(Directory.GetCurrentDirectory(), requestedPath, out fullPath, out error);

    /// <summary>
    /// Resolves <paramref name="requestedPath"/> (relative or absolute, as supplied by the
    /// LLM) against <paramref name="root"/> and confirms the result is still inside
    /// <paramref name="root"/>. Rejects "../" traversal and absolute paths that point
    /// elsewhere on the filesystem.
    /// </summary>
    /// <param name="root">The sandbox root every resolved path must stay within.</param>
    /// <param name="requestedPath">The untrusted path argument from the tool call.</param>
    /// <param name="fullPath">The resolved, validated absolute path. Empty if resolution failed.</param>
    /// <param name="error">A message describing why resolution failed, or null on success.</param>
    public static bool TryResolve(string root, string? requestedPath, out string fullPath, out string? error)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(requestedPath))
        {
            error = "The 'path' argument is required.";
            return false;
        }

        string rootFull = Path.GetFullPath(root);
        string candidate;
        try
        {
            candidate = Path.GetFullPath(Path.Combine(rootFull, requestedPath));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            error = $"Invalid path \"{requestedPath}\": {ex.Message}";
            return false;
        }

        string rootWithSeparator = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        bool inside = candidate.Equals(rootFull, StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);

        if (!inside)
        {
            error = $"Path \"{requestedPath}\" escapes the allowed working directory.";
            return false;
        }

        fullPath = candidate;
        error = null;
        return true;
    }
}
