using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.ReadFile;

internal sealed class ReadFileArguments
{
    public required string Path { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ReadFileArguments))]
internal sealed partial class ReadFileJsonContext : JsonSerializerContext
{
}
