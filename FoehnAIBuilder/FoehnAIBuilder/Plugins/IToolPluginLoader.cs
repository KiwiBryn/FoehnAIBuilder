// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;

namespace FoehnAIBuilder.Plugins;

/// <summary>
/// Discovers and instantiates <see cref="ITool"/> plugin implementations from DLLs on disk.
/// </summary>
public interface IToolPluginLoader
{
    /// <summary>
    /// Scans the configured plugins directory for DLLs, loads every one it finds, and
    /// instantiates every public, non-abstract type that implements <see cref="ITool"/>.
    /// Individual assembly/type failures are logged and skipped rather than aborting startup.
    /// </summary>
    IReadOnlyList<ITool> LoadPlugins();
}
