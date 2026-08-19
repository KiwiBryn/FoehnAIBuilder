namespace FoehnAIBuilder.Abstractions;

/// <summary>
/// Contract implemented by every FoehnAI tool plugin DLL. FoehnAI scans its
/// plugins directory at   startup, loads every assembly it finds, and registers every
/// public, non-abstract type that implements this interface so the Mistral LLM can
/// invoke it as a function-calling tool.
/// </summary>
public interface ITool
{
    /// <summary>
    /// The function name the LLM uses to invoke this tool (e.g. "read_file"). Must be
    /// unique across all loaded plugins and match Mistral's function-name constraints
    /// (letters, digits, underscores, hyphens).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// User-friendly text explaining what the tool does. Shown to humans (tool listings,
    /// logs) and sent to the LLM as the function's description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// A JSON Schema (object) string describing the arguments the LLM must supply when
    /// invoking this tool - i.e. how the LLM should invoke it. Sent to Mistral as the
    /// function's "parameters" field, and used by the tool itself to unpack the JSON
    /// arguments it receives in <see cref="ExecuteAsync"/>.
    /// </summary>
    string Command { get; }

    /// <summary>
    /// How risky this tool's side effects are. The host uses this to decide whether to
    /// prompt the user for confirmation before letting the LLM invoke it.
    /// </summary>
    ToolRiskLevel RiskLevel { get; }

    /// <summary>
    /// Executes the tool with the given JSON-encoded arguments (matching the schema
    /// returned by <see cref="Command"/>).
    /// </summary>
    /// <param name="argumentsJson">JSON object string with the tool's arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the call succeeded and a result the LLM can use to understand what happened.</returns>
    Task<ToolExecutionResult> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default);
}
