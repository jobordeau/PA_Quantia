namespace Quantia.Models;

public class TradeSignal
{
    public DateTime Timestamp { get; set; }
    public string Symbol { get; set; } = "BTCUSDT";
    public string Side { get; set; } = "BUY";
    public decimal Probability { get; set; }
    public decimal Entry { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal PositionSize { get; set; }
    public decimal Confidence { get; set; }
    public string? Note { get; set; }
    public string? Strategy { get; set; }

    public decimal RiskReward =>
        StopLoss == 0 ? 0 :
        Math.Round(Math.Abs(TakeProfit - Entry) /
                   Math.Abs(Entry - StopLoss), 2);
}
