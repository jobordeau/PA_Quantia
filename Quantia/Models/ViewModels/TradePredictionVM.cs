namespace Quantia.Models.ViewModels;

public class TradePredictionVM
{
    public List<DateTime> EquityDates { get; set; } = new();
    public List<decimal> EquityValues { get; set; } = new();

    public List<TradeSignal> Signals { get; set; } = new();

    public TradeSuggestion? Suggestion { get; set; }

    public decimal Balance { get; set; }
    public decimal UnrealizedPnl { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }

    public IEnumerable<TradeModel> ExecutedTrades { get; set; } = Array.Empty<TradeModel>();

    public PortfolioStats Stats
    {
        get => new()
        {
            Balance = Balance,
            UnrealizedPnL = UnrealizedPnl,
            WinRate = WinRate,
            ProfitFactor = ProfitFactor
        };
        set
        {
            if (value == null) return;
            Balance = value.Balance;
            UnrealizedPnl = value.UnrealizedPnL;
            WinRate = value.WinRate;
            ProfitFactor = value.ProfitFactor;
        }
    }
}
