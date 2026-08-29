// Copyright (c) August 2026, devMobile Software
// 
namespace FoehnAIBuilder.Tool.Scan;


internal sealed class ScanArguments
{
    public string? Path { get; init; }
    public string? Pattern { get; init; }
    public bool? Recursive { get; init; }
    public string[]? IgnoreFiles { get; init; }
    public string[]? IgnoreExtensions { get; init; }
    public string[]? IgnoreFolders { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ScanArguments))]
internal sealed partial class ScanJsonContext : JsonSerializerContext
{
}
