using Quantia.Services;

namespace Quantia.Data;

public interface ISentimentRepository
{
    Task<SentimentDto?> GetLatestAsync(CancellationToken ct = default);
}
