using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quantia.Models;

[Table("transactions")]
public class Transaction
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    [Required]
    public int UserId { get; set; }

    [Column("crypto_symbol")]
    [Required]
    [MaxLength(10)]
    public string CryptoSymbol { get; set; } = string.Empty;

    [Column("amount")]
    [Required]
    public decimal Amount { get; set; }

    [Column("price_at_purchase")]
    [Required]
    public decimal PriceAtPurchase { get; set; }

    [Column("timestamp")]
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public UserModel? User { get; set; }

    [Column("side")]
    public string Side { get; set; } = "Buy";

    [Column("trade_id")]
    public Guid TradeId { get; set; }
}
