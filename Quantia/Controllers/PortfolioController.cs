using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quantia.Data;
using Quantia.Models;
using Quantia.Models.ViewModels;
using Quantia.Services;
using System.Security.Claims;

namespace Quantia.Controllers;

[Authorize]
public class PortfolioController : Controller
{
    private readonly AppDbContext _context;
    private readonly PortfolioPriceService _priceService;

    public PortfolioController(AppDbContext context, PortfolioPriceService priceService)
    {
        _context = context;
        _priceService = priceService;
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();

        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync();

        var grouped = transactions
            .GroupBy(t => t.CryptoSymbol)
            .Select(g => new
            {
                Symbol = g.Key,
                Quantity = g.Sum(t => t.Amount),
                Invested = g.Sum(t => t.Amount * t.PriceAtPurchase)
            });

        var portfolio = new List<PortfolioRow>();

        foreach (var g in grouped)
        {
            var latestPrice = await _priceService.GetLatestPrice(g.Symbol);
            if (latestPrice is null) continue;

            var currentValue = g.Quantity * latestPrice.Value;

            portfolio.Add(new PortfolioRow
            {
                Symbol = g.Symbol,
                Quantity = g.Quantity,
                Invested = g.Invested,
                CurrentPrice = latestPrice.Value,
                CurrentValue = currentValue,
                PnL = currentValue - g.Invested
            });
        }

        return View(portfolio);
    }

    [HttpGet]
    public IActionResult Create() => View(new NewTransactionModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NewTransactionModel m)
    {
        if (!ModelState.IsValid) return View(m);

        var userId = CurrentUserId();

        if (m.PriceAtPurchase is null or 0)
        {
            var price = await _priceService.GetLatestPrice(m.CryptoSymbol.ToUpper());
            if (price is null)
            {
                ModelState.AddModelError("", $"Price for {m.CryptoSymbol} not found.");
                return View(m);
            }
            m.PriceAtPurchase = price.Value;
        }

        _context.Transactions.Add(new Transaction
        {
            UserId = userId,
            CryptoSymbol = m.CryptoSymbol.ToUpper(),
            Amount = m.Amount,
            PriceAtPurchase = m.PriceAtPurchase ?? 0,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Simulate() => View(new NewTransactionModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate(NewTransactionModel m, string tradeType, DateTime tradeDate)
    {
        var userId = CurrentUserId();

        if (m.PriceAtPurchase is null or 0)
        {
            var price = await _priceService.GetLatestPrice(m.CryptoSymbol.ToUpper());
            if (price is null)
            {
                ModelState.AddModelError("", $"Price for {m.CryptoSymbol} not found.");
                return View(m);
            }
            m.PriceAtPurchase = price.Value;
        }

        var qty = tradeType == "Sell" ? -Math.Abs(m.Amount) : Math.Abs(m.Amount);

        _context.Transactions.Add(new Transaction
        {
            UserId = userId,
            CryptoSymbol = m.CryptoSymbol.ToUpper(),
            Amount = qty,
            PriceAtPurchase = m.PriceAtPurchase ?? 0,
            Timestamp = DateTime.SpecifyKind(tradeDate, DateTimeKind.Utc)
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
