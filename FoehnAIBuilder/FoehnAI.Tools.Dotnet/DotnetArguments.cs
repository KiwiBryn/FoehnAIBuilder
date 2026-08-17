using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.ExecuteSync;

internal sealed class DotnetArguments
{
    public required string Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public int? TimeoutSeconds { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DotnetArguments))]
internal sealed partial class DotnetJsonContext : JsonSerializerContext
{
}
