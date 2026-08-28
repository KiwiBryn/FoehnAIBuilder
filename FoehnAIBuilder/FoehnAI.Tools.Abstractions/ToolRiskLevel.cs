// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAIBuilder.Abstractions;

/// <summary>
/// Classifies how risky a tool's side effects are, so the host can decide whether to
/// prompt the user for confirmation before letting the LLM invoke it.
/// </summary>
public enum ToolRiskLevel
{
    /// <summary>Risk not declared by the tool. Treated at least as cautiously as <see cref="Destructive"/>.</summary>
    Undefined = 0,

    /// <summary>The tool only inspects state - it cannot modify the file system or run other programs.</summary>
    ReadOnly,

   /// <summary>The tool is a an application that is very limited in scope and is not expected to modify state in a destructive way. The host trusts the tool to be safe.</summary>
   Trusted,

   /// <summary>The tool creates or modifies state (e.g. writing a file, creating a directory) without removing existing data.</summary>
   Write,

    /// <summary>The tool can remove or overwrite state in a way that is hard or impossible to undo.</summary>
    Destructive,
}
