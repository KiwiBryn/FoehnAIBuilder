// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAI.Tools.Rename;

internal sealed class RenameArguments
{
    public required string SourcePath { get; init; }
    public required string DestinationPath { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RenameArguments))]
internal sealed partial class RenameJsonContext : JsonSerializerContext
{
}
