using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Quantia.Services;

public class CryptoPriceService : ICryptoPriceService
{
    private readonly HttpClient _http;
    private readonly ILogger<CryptoPriceService> _log;

    public CryptoPriceService(HttpClient http, ILogger<CryptoPriceService> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<decimal?> GetLastPriceAsync(string symbol)
    {
        try
        {
            var resp = await _http.GetFromJsonAsync<LastCandleResponse>(
                $"/data/{symbol}/last_candle");
            return resp?.PriceUsdt;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to fetch last price for {Symbol}", symbol);
            return null;
        }
    }

    private sealed class LastCandleResponse
    {
        [JsonPropertyName("price_usdt")]
        public decimal PriceUsdt { get; set; }
    }
}
