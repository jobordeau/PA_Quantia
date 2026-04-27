using System.ComponentModel.DataAnnotations;

namespace Quantia.Models.ViewModels;

public class NewTransactionModel
{
    [Required, MaxLength(10)]
    public string CryptoSymbol { get; set; } = string.Empty;

    [Required, Range(0.00000001, double.MaxValue)]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? PriceAtPurchase { get; set; }
}
