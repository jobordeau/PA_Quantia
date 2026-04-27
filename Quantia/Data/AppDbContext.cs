using Microsoft.EntityFrameworkCore;
using Quantia.Models;

namespace Quantia.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserModel> Users => Set<UserModel>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TradeModel> Trades => Set<TradeModel>();
    public DbSet<SentimentScore> SentimentScores => Set<SentimentScore>();
    public DbSet<SentimentDetail> SentimentDetails => Set<SentimentDetail>();
}
