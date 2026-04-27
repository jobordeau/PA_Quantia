namespace Quantia.Services;

public sealed class SentimentDto
{
    public double Global_Index { get; init; }
    public List<ClusterDto> Clusters { get; init; } = new();
}

public sealed class ClusterDto
{
    public string Topic { get; init; } = string.Empty;
    public double Avg { get; init; }
    public int Freq { get; init; }
    public double Delta { get; init; }
    public string Summary { get; init; } = string.Empty;
    public List<string> FullMessages { get; init; } = new();
    public List<string> Examples { get; init; } = new();
    public List<string> Urls { get; init; } = new();
}
