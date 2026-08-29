// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAIBuilder.Configuration;

/// <summary>
/// Public, non-secret application settings bound from the "FoehnAIBuilder" section of appsettings.json.
/// </summary>
public sealed class FoehnAIBuilderOptions
{
    /// <summary>
    /// The system message sent to the LLM at the start of every conversation.
    /// </summary>
    public string SystemMessageFile { get; set; } = string.Empty;

    /// <summary>
    /// Directory to scan for tool plugin DLLs at startup, relative to the application's
    /// base directory unless rooted.
    /// </summary>
    public string PluginsPath { get; set; } = ".Plugins";

   /// <summary>
   /// Directory to scan for skill markdown files at startup, relative to the application's 
   /// </summary>
   public string SkillsPath { get; set; } = ".Skills";

   /// <summary>
   /// Working directory tools should operate in when a path isn't explicitly rooted.
   /// Empty means "use the process's current directory".
   /// </summary>
   public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of tool-call round trips per user message before FoehnAI gives
    /// up and returns whatever the model last said, to guard against infinite loops.
    /// </summary>
    public int MaxToolIterations { get; set; } = 25;

    /// <summary>
    /// The foreground color for the console output.
    /// </summary>
    public ConsoleColor ForegroundColour { get; set; } = ConsoleColor.White;

    /// <summary>
    /// The background color for the console output.
    /// </summary>
    public ConsoleColor BackgroundColour { get; set; } = ConsoleColor.Black;

    /// <summary>
    /// Whether to print the results of tool calls to the console.
    /// </summary>
    public bool PrintToolResults { get; set; } = false;
}
