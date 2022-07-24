#nullable disable
using System.Text.Json.Serialization;

namespace Ellie.Modules.Searches;

public class FinnHubSearchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("result")]
    public List<FinnHubSearchResult> Result { get; set; }
}