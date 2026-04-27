using System.ComponentModel.DataAnnotations.Schema;

namespace Quantia.Models;

[Table("sentiment_scores")]
public class SentimentScore
{
    [Column("id")] public long Id { get; set; }
    [Column("score")] public double Score { get; set; }
    [Column("ts_hour")] public DateTime TsHour { get; set; }
    [Column("ts")] public DateTime Ts { get; set; }
    [Column("price_btc")] public double? PriceBtc { get; set; }
    [Column("price_eth")] public double? PriceEth { get; set; }
}
