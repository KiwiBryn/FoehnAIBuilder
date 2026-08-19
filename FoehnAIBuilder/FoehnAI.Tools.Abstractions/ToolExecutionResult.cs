// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAIBuilder.Abstractions;

/// <summary>
/// The outcome of an <see cref="ITool"/> invocation, returned to the LLM so it can
/// understand what happened and decide how to proceed.
/// </summary>
public sealed class ToolExecutionResult
{
    /// <summary>
    /// Whether the tool invocation completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Human/LLM-readable description of the outcome (data returned, error message, etc.).
    /// </summary>
    public required string Result { get; init; }

    public static ToolExecutionResult Ok(string result) => new() { Success = true, Result = result };

    public static ToolExecutionResult Fail(string result) => new() { Success = false, Result = result };
}
