using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TradeIngestionAssignment.Data;
using TradeIngestionAssignment.Domain;
using TradeIngestionAssignment.Options;
using TradeIngestionAssignment.Services;
using Xunit;

namespace TradeIngestionAssignment.Tests;

public class TradeIngestionFlowTests
{
    [Fact]
    public async Task Ingestion_Duplicate_And_Correction_ProduceExpectedSnapshot() 
    {
        var dbOptions = new DbContextOptionsBuilder<TradeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TradeDbContext(dbOptions);

        dbContext.PriceQuotes.Add(new PriceQuote
        {
            Id = Guid.NewGuid(),
            Isin = "US0378331005",
            Currency = "USD",
            PriceDate = new DateOnly(2025, 3, 1),
            PriceUsd = 186m
        });
        await dbContext.SaveChangesAsync();

        var options = Microsoft.Extensions.Options.Options.Create(new TradeProcessingOptions
        {
            EnableDuplicateCorrectionHandling = true
        });

        var ingestionService = new TradeIngestionService(dbContext, options, NullLogger<TradeIngestionService>.Instance);
        var snapshotService = new PortfolioSnapshotService(dbContext, options, NullLogger<PortfolioSnapshotService>.Instance);

        await ingestionService.IngestAsync(new TradeSubmissionCommand
        {
            ExternalRef = "TRX-1001",
            AccountId = "ACC-001",
            Isin = "US0378331005",
            Symbol = "AAPL",
            Side = TradeSide.Buy,
            Quantity = 120m,
            Price = 185.40m,
            TradeDate = new DateOnly(2025, 3, 1),
            AsOfUtc = DateTime.Parse("2025-03-01T10:15:00Z").ToUniversalTime()
        }, CancellationToken.None);

        await ingestionService.IngestAsync(new TradeSubmissionCommand
        {
            ExternalRef = "TRX-1001",
            AccountId = "ACC-001",
            Isin = "US0378331005",
            Symbol = "AAPL",
            Side = TradeSide.Buy,
            Quantity = 120m,
            Price = 185.40m,
            TradeDate = new DateOnly(2025, 3, 1),
            AsOfUtc = DateTime.Parse("2025-03-01T10:15:00Z").ToUniversalTime()
        }, CancellationToken.None);

        await ingestionService.IngestAsync(new TradeSubmissionCommand
        {
            ExternalRef = "TRX-1001",
            AccountId = "ACC-001",
            Isin = "US0378331005",
            Symbol = "AAPL",
            Side = TradeSide.Buy,
            Quantity = 100m,
            Price = 185.40m,
            TradeDate = new DateOnly(2025, 3, 1),
            AsOfUtc = DateTime.Parse("2025-03-01T12:00:00Z").ToUniversalTime()
        }, CancellationToken.None);

        var snapshot = await snapshotService.GetSnapshotAsync("ACC-001", new DateOnly(2025, 3, 1), CancellationToken.None);

        Assert.Equal(3, dbContext.TradeEvents.Count());
        Assert.Single(dbContext.AppliedTrades);
        Assert.Single(snapshot.Positions);

        var position = snapshot.Positions[0];
        Assert.Equal(100m, position.Quantity);
        Assert.Equal(18600m, position.MarketValueUsd);
        Assert.Equal(18600m, snapshot.TotalMarketValueUsd);
    }
}
