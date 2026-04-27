using System.Text.Json.Serialization;

namespace Quantia.Models;

public class PredictionSignal
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("prob_up")]
    public float ProbUp { get; set; }

    [JsonPropertyName("signal")]
    public string Signal { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    public bool UsingIncompleteCandle { get; set; }
}
