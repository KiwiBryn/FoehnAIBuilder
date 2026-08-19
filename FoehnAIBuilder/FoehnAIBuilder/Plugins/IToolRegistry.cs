// Copyright (c) August 2026, devMobile Software
// 
using FoehnAIBuilder.Abstractions;
using MistralAI.Client.DTOs.Shared;

namespace FoehnAIBuilder.Plugins;

/// <summary>
/// Holds the set of loaded <see cref="ITool"/> plugins and adapts them to/from the
/// Mistral chat completion API's function-calling shape.
/// </summary>
public interface IToolRegistry
{
    /// <summary>All tools currently registered.</summary>
    IReadOnlyList<ITool> Tools { get; }

    /// <summary>Builds the Mistral <see cref="Tool"/> DTOs to send with a chat completion request.</summary>
    List<Tool> BuildToolDefinitions();

    /// <summary>Executes the named tool with the given JSON arguments.</summary>
    Task<ToolExecutionResult> ExecuteAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}
