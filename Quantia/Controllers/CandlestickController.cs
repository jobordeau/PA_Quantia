using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Web;

namespace Quantia.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CandlestickController : ControllerBase
{
    private readonly HttpClient _http;

    public CandlestickController(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("MLApi");
    }

    private async Task<IActionResult> ProxyAsync(string relativeUrl)
    {
        var resp = await _http.GetAsync(relativeUrl);
        if (!resp.IsSuccessStatusCode)
        {
            return StatusCode((int)resp.StatusCode,
                "Failed to retrieve data from backend API.");
        }

        var json = await resp.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    [HttpGet("load")]
    public Task<IActionResult> LoadCandles(
        [FromQuery] string symbol,
        [FromQuery] string start_date,
        [FromQuery] string end_date)
    {
        var q = $"?symbol={HttpUtility.UrlEncode(symbol)}" +
                $"&start_date={HttpUtility.UrlEncode(start_date)}" +
                $"&end_date={HttpUtility.UrlEncode(end_date)}";

        return ProxyAsync($"/pattern/load-data{q}");
    }

    [HttpGet("predict")]
    public Task<IActionResult> GetPredictions([FromQuery] string symbol)
    {
        var q = $"?symbol={HttpUtility.UrlEncode(symbol)}";
        return ProxyAsync($"/prediction/latest{q}");
    }

    [HttpGet("patterns")]
    public Task<IActionResult> LoadPatterns(
        [FromQuery] string symbol,
        [FromQuery] string start_date,
        [FromQuery] string end_date)
    {
        var q = $"?symbol={HttpUtility.UrlEncode(symbol)}" +
                $"&start_date={HttpUtility.UrlEncode(start_date)}" +
                $"&end_date={HttpUtility.UrlEncode(end_date)}";

        return ProxyAsync($"/pattern/load-data-patterns{q}");
    }

    [HttpGet("patterns/classic")]
    public Task<IActionResult> LoadClassicPatterns(
        [FromQuery] string symbol,
        [FromQuery] string start_date,
        [FromQuery] string end_date,
        [FromQuery] double atr_min_pct = 0.05)
    {
        var q = $"?symbol={HttpUtility.UrlEncode(symbol)}" +
                $"&start_date={HttpUtility.UrlEncode(start_date)}" +
                $"&end_date={HttpUtility.UrlEncode(end_date)}" +
                $"&atr_min_pct={atr_min_pct.ToString(CultureInfo.InvariantCulture)}";

        return ProxyAsync($"/pattern/load-data-patterns-classic{q}");
    }
}
