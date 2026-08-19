using System.Text.Json.Serialization;

namespace FoehnAI.Tools.SearchFiles;

internal sealed class SearchFilesArguments
{
    public string? Path { get; init; }
    public string? Text { get; init; }
    public string? Pattern { get; init; }
    public bool? CaseSensitive { get; init; }
    public bool? Recursive { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SearchFilesArguments))]
internal sealed partial class SearchFilesJsonContext : JsonSerializerContext
{
}
