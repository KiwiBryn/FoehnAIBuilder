using System;
using System.Collections.Generic;

namespace FoehnAIBuilder.Tools.Scan;

/// <summary>
/// Configuration options for the Scan tool, bound from the "ScanTool" section of appsettings.json.
/// </summary>
public sealed class ScanToolOptions
{
    /// <summary>
    /// Default search pattern for the scan tool.
    /// </summary>
    public string DefaultPattern { get; set; } = "*";

    /// <summary>
    /// Default value for recursive scanning.
    /// </summary>
    public bool DefaultRecursive { get; set; } = true;

    /// <summary>
    /// Default list of file names to ignore during scanning.
    /// </summary>
    public List<string> DefaultIgnoreFiles { get; set; } = new List<string>();

    /// <summary>
    /// Default list of file extensions to ignore during scanning.
    /// </summary>
    public List<string> DefaultIgnoreExtensions { get; set; } = new List<string>();

    /// <summary>
    /// Default list of folder names to ignore during scanning.
    /// </summary>
    public List<string> DefaultIgnoreFolders { get; set; } = new List<string>();

    /// <summary>
    /// Maximum number of entries to return in a scan result.
    /// </summary>
    public int MaxEntries { get; set; } = 2000;
}