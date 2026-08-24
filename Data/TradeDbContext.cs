using Microsoft.EntityFrameworkCore;
using TradeIngestionAssignment.Domain;

namespace TradeIngestionAssignment.Data;

public class TradeDbContext(DbContextOptions<TradeDbContext> options) : DbContext(options)
{
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<TradeEvent> TradeEvents => Set<TradeEvent>();
    public DbSet<AppliedTrade> AppliedTrades => Set<AppliedTrade>();
    public DbSet<PriceQuote> PriceQuotes => Set<PriceQuote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.ToTable("Instruments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Isin).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Isin).IsUnique().HasDatabaseName("UX_Instruments_Isin");
        });

        modelBuilder.Entity<TradeEvent>(entity =>
        {
            entity.ToTable("TradeEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalRef).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AccountId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Isin).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.Property(x => x.Price).HasPrecision(18, 6);
            entity.HasIndex(x => new { x.ExternalRef, x.AsOfUtc }).HasDatabaseName("IX_TradeEvents_ExternalRef_AsOfUtc");
        });

        modelBuilder.Entity<AppliedTrade>(entity =>
        {
            entity.ToTable("AppliedTrades");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ExternalRef).HasMaxLength(64).IsRequired();
            entity.Property(x => x.AccountId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Isin).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 6);
            entity.Property(x => x.Price).HasPrecision(18, 6);
            entity.HasIndex(x => x.ExternalRef).IsUnique().HasDatabaseName("UX_AppliedTrades_ExternalRef");
            entity.HasIndex(x => new { x.AccountId, x.TradeDate }).HasDatabaseName("IX_AppliedTrades_Account_TradeDate");

            entity.HasOne(x => x.LatestTradeEvent)
                .WithMany()
                .HasForeignKey(x => x.LatestTradeEventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PriceQuote>(entity =>
        {
            entity.ToTable("PriceQuotes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Isin).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.PriceUsd).HasPrecision(18, 6);
            entity.HasIndex(x => new { x.Isin, x.PriceDate }).HasDatabaseName("IX_PriceQuotes_Isin_PriceDate");
        });
    }
}
