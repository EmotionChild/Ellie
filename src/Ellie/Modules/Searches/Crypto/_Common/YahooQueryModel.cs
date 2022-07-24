using System.Text.Json.Serialization;

namespace Ellie.Modules.Searches;

public class YahooQueryModel
{
    [JsonPropertyName("quoteResponse")]
    public QuoteResponse QuoteResponse { get; set; } = null!;
}