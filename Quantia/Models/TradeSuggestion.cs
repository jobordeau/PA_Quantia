namespace Quantia.Models;

public class TradeSuggestion
{
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal PositionSize { get; set; }
    public decimal Confidence { get; set; }
    public DateTime Timestamp { get; set; }
}
