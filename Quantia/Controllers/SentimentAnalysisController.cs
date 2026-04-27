using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantia.Services;

namespace Quantia.Controllers;

[Authorize]
public sealed class SentimentAnalysisController : Controller
{
    private readonly SentimentService _service;

    public SentimentAnalysisController(SentimentService service) => _service = service;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var dto = await _service.GetLatestAsync(ct);
            return View(dto);
        }
        catch (Exception ex)
        {
            return Content($"Sentiment data not available ({ex.Message}).");
        }
    }
}
