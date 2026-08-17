using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.Mkdir;

internal sealed class MkdirArguments
{
    public required string Path { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MkdirArguments))]
internal sealed partial class MkdirJsonContext : JsonSerializerContext
{
}
