using System.Text.Json.Serialization;

namespace Ellie.Services;

public sealed class GoogleImageData
{
    [JsonPropertyName("contextLink")]
    public string ContextLink { get; init; } = null!;

    [JsonPropertyName("thumbnailLink")]
    public string ThumbnailLink { get; init; } = null!;
}