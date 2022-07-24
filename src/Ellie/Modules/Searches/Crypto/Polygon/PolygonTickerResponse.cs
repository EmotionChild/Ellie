#nullable disable
using System.Text.Json.Serialization;

namespace Ellie.Modules.Searches;

public class PolygonTickerResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
    
    [JsonPropertyName("results")]
    public List<PolygonTickerData> Results { get; set; }
}