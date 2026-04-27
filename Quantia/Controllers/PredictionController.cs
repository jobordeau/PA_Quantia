using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Data;
using Quantia.Models;
using Quantia.Models.ViewModels;
using Quantia.Services;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Quantia.Controllers;

[Authorize]
[Route("Prediction")]
public class PredictionController : Controller
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly PortfolioEquityService _equityService;
    private readonly IHttpContextAccessor _httpContext;
    private readonly AppDbContext _db;

    public PredictionController(
        IHttpClientFactory httpFactory,
        PortfolioEquityService equityService,
        IHttpContextAccessor httpContextAccessor,
        AppDbContext dbContext)
    {
        _httpFactory = httpFactory;
        _equityService = equityService;
        _httpContext = httpContextAccessor;
        _db = dbContext;
    }

    private HttpClient MlClient => _httpFactory.CreateClient("MLApi");

    [HttpGet("")]
    public async Task<IActionResult> Index(string symbol = "BTCUSDT")
        => View("Index", await BuildViewModel(symbol));

    [HttpGet("json")]
    public async Task<IActionResult> GetJson(string symbol = "BTCUSDT")
        => Json(await BuildViewModel(symbol));

    private async Task<TradePredictionVM> BuildViewModel(string symbol)
    {
        PredictionResponse? raw = null;
        try
        {
            raw = await MlClient.GetFromJsonAsync<PredictionResponse>(
                $"/prediction/latest?symbol={symbol}");
        }
        catch
        {
        }

        var signals = new List<TradeSignal>();
        if (raw is not null)
        {
            signals.Add(new TradeSignal
            {
                Timestamp = raw.Timestamp,
                Symbol = raw.Symbol,
                Probability = (decimal)raw.ProbUp,
                Side = raw.Signal == "LONG" ? "BUY" : "SELL",
                Entry = raw.Entry,
                StopLoss = raw.StopLoss,
                TakeProfit = raw.TakeProfit,
                Confidence = (decimal)raw.Confidence,
                PositionSize = (decimal)raw.Confidence * 100,
                Note = raw.Note,
                Strategy = "auto_sl_tp"
            });
        }

        var userId = int.Parse(_httpContext.HttpContext!
            .User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var trades = await _db.Trades
            .Where(t => t.UserId == userId && t.CryptoSymbol == symbol)
            .OrderBy(t => t.BuyDate)
            .ToListAsync();

        var (dates, values) = await _equityService.GetEquityAsync(userId, 30);
        var stats = await _equityService.GetStatsAsync(userId);

        return new TradePredictionVM
        {
            EquityDates = dates,
            EquityValues = values,
            Balance = stats.Balance,
            UnrealizedPnl = stats.UnrealizedPnL,
            WinRate = stats.WinRate,
            ProfitFactor = stats.ProfitFactor,
            Signals = signals,
            ExecutedTrades = trades
        };
    }

    [HttpPost("RefreshModel")]
    public async Task<IActionResult> RefreshModel()
    {
        var resp = await MlClient.PostAsync("/refresh-model", null);
        return Content(await resp.Content.ReadAsStringAsync(), "application/json");
    }

    [HttpPost("RunMlPipeline")]
    public async Task<IActionResult> RunMlPipeline([FromBody] PipelineRequest dto)
    {
        var resp = await MlClient.PostAsJsonAsync("/run_ml_pipeline", dto);
        return Content(await resp.Content.ReadAsStringAsync(), "application/json");
    }

    [HttpGet("GetModelMetrics")]
    public async Task<IActionResult> GetMetrics(string? model)
    {
        var url = "/get_model_metrics";
        if (!string.IsNullOrWhiteSpace(model)) url += $"?model={model}";
        var json = await MlClient.GetStringAsync(url);
        return Content(json, "application/json");
    }

    public record PipelineRequest(string Mode, string Symbol, int Days, string? ModelPath);
    public record PredictRequest(string ApiUrl, string Symbol);

    [HttpPost("GetPredictions")]
    public async Task<IActionResult> GetPredictions([FromBody] PredictRequest dto)
    {
        try
        {
            var url = $"{dto.ApiUrl}?symbol={dto.Symbol}";
            using var raw = _httpFactory.CreateClient();
            var json = await raw.GetStringAsync(url);
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return Problem($"Error fetching predictions: {ex.Message}");
        }
    }
}
