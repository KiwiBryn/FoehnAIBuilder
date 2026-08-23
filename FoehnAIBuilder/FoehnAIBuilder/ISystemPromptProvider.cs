// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAIBuilder.Chat;

/// <summary>
/// Supplies the system prompt used to seed a new <see cref="AgentSession"/>.
/// Implementations may load from disk, configuration, embedded resources, a
/// remote service, etc. Return <c>null</c> or whitespace to run the session
/// without a system prompt.
/// </summary>
public interface ISystemPromptProvider
{
    /// <summary>
    /// Returns the system prompt text, or <c>null</c>/whitespace if none is available.
    /// </summary>
    string? SystemPrompt();
   string? SystemPromptFilename();
}