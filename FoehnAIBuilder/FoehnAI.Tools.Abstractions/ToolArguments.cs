using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace FoehnAIBuilder.Abstractions;

/// <summary>
/// Deserializes a tool's JSON arguments into a strongly-typed DTO using a source-generated
/// <see cref="JsonTypeInfo{T}"/>, so tools don't need to hand-walk a <see cref="JsonDocument"/>
/// and each argument's name/type is checked by the compiler instead of a string lookup.
/// </summary>
public static class ToolArguments
{
    /// <param name="argumentsJson">The raw JSON object string received by the tool. Empty/whitespace is treated as "{}".</param>
    /// <param name="typeInfo">The source-generated type info for <typeparamref name="T"/> (e.g. <c>MyToolJsonContext.Default.MyToolArguments</c>).</param>
    /// <param name="arguments">The deserialized arguments on success; <see langword="null"/>! on failure.</param>
    /// <param name="error">A message suitable for <see cref="ToolExecutionResult.Fail"/> on failure; <see langword="null"/> on success.</param>
    public static bool TryParse<T>(string argumentsJson, JsonTypeInfo<T> typeInfo, out T arguments, out string? error)
        where T : class
    {
        try
        {
            var json = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson;
            var parsed = JsonSerializer.Deserialize(json, typeInfo);
            if (parsed is null)
            {
                arguments = null!;
                error = "Arguments JSON must be a JSON object.";
                return false;
            }

            arguments = parsed;
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            arguments = null!;
            error = $"Invalid arguments JSON: {ex.Message}";
            return false;
        }
    }
}
