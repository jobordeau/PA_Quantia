using System.Net.Http.Headers;
using System.Net.Http.Json;
using Quantia.Models;

namespace Quantia.Services;

public class TradeSuggestionService
{
    private readonly HttpClient _http;
    private readonly ILogger<TradeSuggestionService> _log;

    public TradeSuggestionService(HttpClient http, ILogger<TradeSuggestionService> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<TradeSuggestion?> GetSuggestionAsync(
        string symbol,
        decimal riskMultiple,
        string? jwt = null)
    {
        var path = $"/trade/suggest?symbol={symbol}&risk_multiple={riskMultiple}";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(jwt))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }

        try
        {
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<TradeSuggestion>();
        }
        catch (HttpRequestException ex)
        {
            _log.LogWarning(ex, "TradeSuggestion request failed for {Symbol}", symbol);
            return null;
        }
    }
}
