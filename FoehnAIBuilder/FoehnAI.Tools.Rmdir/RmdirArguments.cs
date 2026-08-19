// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAI.Tools.Rmdir;

internal sealed class RmdirArguments
{
    public required string Path { get; init; }
    public bool? Recursive { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RmdirArguments))]
internal sealed partial class RmdirJsonContext : JsonSerializerContext
{
}
