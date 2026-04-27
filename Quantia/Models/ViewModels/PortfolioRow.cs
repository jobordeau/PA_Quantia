namespace Quantia.Models.ViewModels;

public class PortfolioRow
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Invested { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal PnL { get; set; }
}
