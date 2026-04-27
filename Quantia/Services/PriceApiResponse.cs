using System.Text.Json.Serialization;

namespace Quantia.Services;

public class PriceApiResponse
{
    [JsonPropertyName("data")]
    public List<CryptoPricePoint> Data { get; set; } = new();
}

public class CryptoPricePoint
{
    [JsonPropertyName("timestamp_utc")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("close")]
    public decimal Price { get; set; }
}
