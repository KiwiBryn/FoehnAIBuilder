using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.ExecuteAsync;

internal sealed class ExecuteDetachedArguments
{
    public required string Command { get; init; }
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExecuteDetachedArguments))]
internal sealed partial class ExecuteDetachedJsonContext : JsonSerializerContext
{
}
