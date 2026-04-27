namespace Quantia.Services;

public interface ICryptoPriceService
{
    Task<decimal?> GetLastPriceAsync(string symbol);
}
