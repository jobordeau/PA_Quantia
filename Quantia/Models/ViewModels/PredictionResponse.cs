using System.Text.Json.Serialization;

namespace Quantia.Models.ViewModels;

public class PredictionResponse
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("prob_up")]
    public double ProbUp { get; set; }

    [JsonPropertyName("signal")]
    public string Signal { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("entry")]
    public decimal Entry { get; set; }

    [JsonPropertyName("stop_loss")]
    public decimal StopLoss { get; set; }

    [JsonPropertyName("take_profit")]
    public decimal TakeProfit { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
