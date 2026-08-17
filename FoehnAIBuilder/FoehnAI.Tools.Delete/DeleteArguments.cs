using System.Text.Json.Serialization;

namespace FoehnSharp.Tools.Delete;

internal sealed class DeleteArguments
{
    public required string Path { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DeleteArguments))]
internal sealed partial class DeleteJsonContext : JsonSerializerContext
{
}
