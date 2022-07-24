using System.Text.Json.Serialization;

namespace Ellie.Modules.Searches;

public sealed class InvidiousSearchResponse
{
    [JsonPropertyName("videoId")]
    public string VideoId { get; set; } = null!;
}