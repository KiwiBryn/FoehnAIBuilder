using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.ExecuteSync;

internal sealed class ExecuteArguments
{
    public required string Command { get; init; }
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public int? TimeoutSeconds { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ExecuteArguments))]
internal sealed partial class ExecuteJsonContext : JsonSerializerContext
{
}
