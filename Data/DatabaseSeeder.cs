using Microsoft.EntityFrameworkCore;
using TradeIngestionAssignment.Domain;

namespace TradeIngestionAssignment.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(TradeDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await dbContext.Instruments.AnyAsync(cancellationToken))
        {
            dbContext.Instruments.Add(new Instrument
            {
                Id = Guid.NewGuid(),
                Isin = "US0378331005",
                Symbol = "AAPL",
                Name = "Apple Inc."
            });
        }

        if (!await dbContext.PriceQuotes.AnyAsync(cancellationToken))
        {
            dbContext.PriceQuotes.Add(new PriceQuote
            {
                Id = Guid.NewGuid(),
                Isin = "US0378331005",
                PriceDate = new DateOnly(2025, 3, 1),
                PriceUsd = 186.00m,
                Currency = "USD"
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
