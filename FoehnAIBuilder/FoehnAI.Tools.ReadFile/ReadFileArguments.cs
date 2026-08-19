// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAI.Tools.ReadFile;

internal sealed class ReadFileArguments
{
    public required string Path { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ReadFileArguments))]
internal sealed partial class ReadFileJsonContext : JsonSerializerContext
{
}
