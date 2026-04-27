using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Data;
using System.Security.Claims;

namespace Quantia.Controllers;

[Authorize]
public class TradeHistoryController : Controller
{
    private readonly AppDbContext _context;

    public TradeHistoryController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var closedTrades = await _context.Trades
            .Where(t => t.UserId == userId && t.Status == "Closed")
            .OrderByDescending(t => t.SellDate)
            .ToListAsync();

        return View(closedTrades);
    }
}
