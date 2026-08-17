using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.Scan;

internal sealed class ScanArguments
{
    public string? Path { get; init; }
    public string? Pattern { get; init; }
    public bool? Recursive { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ScanArguments))]
internal sealed partial class ScanJsonContext : JsonSerializerContext
{
}
