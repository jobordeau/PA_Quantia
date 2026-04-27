using Microsoft.EntityFrameworkCore;
using Quantia.Data;
using Quantia.Models.ViewModels;

namespace Quantia.Services;

public class PortfolioEquityService
{
    private readonly AppDbContext _context;
    private readonly PortfolioPriceService _price;

    public PortfolioEquityService(AppDbContext context, PortfolioPriceService priceService)
    {
        _context = context;
        _price = priceService;
    }

    public async Task<(List<DateTime> Dates, List<decimal> Values)> GetEquityAsync(int userId, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var trades = await _context.Trades
            .Where(t => t.UserId == userId && t.BuyDate >= since)
            .OrderBy(t => t.BuyDate)
            .ToListAsync();

        var dates = new List<DateTime>();
        var equity = new List<decimal>();
        decimal balance = 0;

        foreach (var trade in trades)
        {
            var referenceDate = trade.SellDate ?? DateTime.UtcNow;
            var price = await _price.GetHistoricalPrice(trade.CryptoSymbol, referenceDate)
                        ?? trade.BuyPrice;

            decimal value = trade.SellDate is null
                ? trade.Quantity * price
                : trade.Quantity * (trade.SellPrice ?? price);

            balance += value;
            dates.Add(referenceDate);
            equity.Add(balance);
        }

        return (dates, equity);
    }

    public async Task<PortfolioStats> GetStatsAsync(int userId)
    {
        var trades = await _context.Trades
            .Where(t => t.UserId == userId)
            .ToListAsync();

        if (trades.Count == 0) return new PortfolioStats();

        decimal balance = 0, invested = 0, wins = 0, losses = 0;

        foreach (var grp in trades.GroupBy(t => t.CryptoSymbol))
        {
            var last = await _price.GetLatestPrice(grp.Key);
            if (last is null) continue;

            var qty = grp.Where(t => t.SellDate == null).Sum(t => t.Quantity);
            var curVal = qty * last.Value;
            var invVal = grp.Sum(t => t.Quantity * t.BuyPrice);

            balance += curVal;
            invested += invVal;

            var pnl = curVal - invVal;
            if (pnl >= 0) wins += pnl; else losses += -pnl;
        }

        return new PortfolioStats
        {
            Balance = balance,
            UnrealizedPnL = balance - invested,
            WinRate = wins + losses == 0 ? 0 : wins / (wins + losses),
            ProfitFactor = losses == 0 ? wins : wins / losses
        };
    }
}
