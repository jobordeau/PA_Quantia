using Microsoft.EntityFrameworkCore;
using Quantia.Services;
using System.Text.Json;

namespace Quantia.Data;

public sealed class EfSentimentRepository : ISentimentRepository
{
    private readonly AppDbContext _db;

    public EfSentimentRepository(AppDbContext db) => _db = db;

    public async Task<SentimentDto?> GetLatestAsync(CancellationToken ct = default)
    {
        var raw = await _db.SentimentDetails
            .OrderByDescending(d => d.TsHour)
            .Select(d => d.JsonPayload)
            .FirstOrDefaultAsync(ct);

        return raw is null
            ? null
            : JsonSerializer.Deserialize<SentimentDto>(raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
