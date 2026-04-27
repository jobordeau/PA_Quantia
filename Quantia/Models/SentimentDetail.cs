using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quantia.Models;

[Table("sentiment_details")]
public class SentimentDetail
{
    [Key]
    [Column("ts_hour")]
    public DateTime TsHour { get; set; }

    [Column("json_payload", TypeName = "jsonb")]
    public string JsonPayload { get; set; } = "{}";
}
