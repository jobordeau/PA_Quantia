using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Data;

namespace Quantia.Controllers;

[Route("api/sentiment")]
[ApiController]
public class SentimentApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public SentimentApiController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? hours = null)
    {
        var utcNow = DateTime.UtcNow;
        var since = from ?? (hours != null ? utcNow.AddHours(-hours.Value) : utcNow.AddDays(-7));
        var until = to ?? utcNow;

        var rows = await _db.SentimentScores
            .Where(s => s.TsHour >= since && s.TsHour <= until)
            .OrderBy(s => s.TsHour)
            .Select(s => new
            {
                ts = s.TsHour,
                price_btc = s.PriceBtc,
                price_eth = s.PriceEth,
                score = s.Score
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("detail")]
    public async Task<IActionResult> Detail([FromQuery] DateTime ts)
    {
        var hour = ts.ToUniversalTime()
            .AddMinutes(-ts.Minute)
            .AddSeconds(-ts.Second)
            .AddMilliseconds(-ts.Millisecond);

        var payload = await _db.SentimentDetails
            .Where(d => d.TsHour == hour)
            .Select(d => d.JsonPayload)
            .FirstOrDefaultAsync();

        if (payload is null) return NotFound();
        return Content(payload, "application/json");
    }
}
