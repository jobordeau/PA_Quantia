using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quantia.Services;

public class PortfolioPriceService
{
    private readonly HttpClient _http;
    private readonly ILogger<PortfolioPriceService> _log;

    public PortfolioPriceService(HttpClient http, ILogger<PortfolioPriceService> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<decimal?> GetLatestPrice(string symbol)
    {
        try
        {
            var json = await _http.GetFromJsonAsync<LastCandleResponse>(
                $"/data/{symbol}/last_candle");
            return json?.PriceUsdt;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "GetLatestPrice failed for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<decimal?> GetHistoricalPrice(string symbol, DateTime utcTime)
    {
        var days = Math.Max(1, (DateTime.UtcNow.Date - utcTime.Date).Days + 1);
        var url = $"/data/{symbol}?days={days}&interval=1m&raw=true";

        var candles = await FetchCandles(url);
        if (candles is null || candles.Count == 0) return null;

        var candle = candles
            .Where(c => DateTime.TryParse(c.TimestampUtc, out var t) && t <= utcTime)
            .OrderByDescending(c => DateTime.Parse(c.TimestampUtc))
            .FirstOrDefault();

        return candle?.Close ?? candle?.Price;
    }

    private async Task<List<Candle>?> FetchCandles(string url)
    {
        try
        {
            var json = await _http.GetStringAsync(url);
            if (string.IsNullOrWhiteSpace(json)) return null;

            json = json.TrimStart();
            if (json.StartsWith("{"))
            {
                var obj = JsonSerializer.Deserialize<DataResponse>(json);
                return obj?.Data;
            }
            if (json.StartsWith("["))
            {
                return JsonSerializer.Deserialize<List<Candle>>(json);
            }
            return null;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "FetchCandles failed for {Url}", url);
            return null;
        }
    }

    private sealed class LastCandleResponse
    {
        [JsonPropertyName("price_usdt")]
        public decimal PriceUsdt { get; set; }
    }

    private sealed class DataResponse
    {
        [JsonPropertyName("data")]
        public List<Candle> Data { get; set; } = new();
    }

    private sealed class Candle
    {
        [JsonPropertyName("timestamp_utc")]
        public string TimestampUtc { get; set; } = string.Empty;

        [JsonPropertyName("close")]
        public decimal? Close { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }
    }
}
