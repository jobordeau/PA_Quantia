namespace Quantia.Models;

public class LiveTradeModel
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Entry { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal Quantity { get; set; }
    public decimal Current { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPct { get; set; }
    public string Status { get; set; } = "Open";
}
