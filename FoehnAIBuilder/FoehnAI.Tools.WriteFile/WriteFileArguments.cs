using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.WriteFile;

internal sealed class WriteFileArguments
{
    public required string Path { get; init; }
    public required string Content { get; init; }
    public bool? Overwrite { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WriteFileArguments))]
internal sealed partial class WriteFileJsonContext : JsonSerializerContext
{
}
