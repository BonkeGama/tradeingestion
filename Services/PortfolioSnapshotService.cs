using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradeIngestionAssignment.Data;
using TradeIngestionAssignment.Domain;
using TradeIngestionAssignment.Options;

namespace TradeIngestionAssignment.Services;

public class PortfolioSnapshotService(
    TradeDbContext dbContext,
    IOptions<TradeProcessingOptions> options,
    ILogger<PortfolioSnapshotService> logger) : IPortfolioSnapshotService
{
    private readonly TradeProcessingOptions _options = options.Value;

    public async Task<PortfolioSnapshotResult> GetSnapshotAsync(string accountId, DateOnly snapshotDate, CancellationToken cancellationToken)
    {
        var trades = _options.EnableDuplicateCorrectionHandling
            ? await dbContext.AppliedTrades
                .AsNoTracking()
                .Where(x => x.AccountId == accountId && x.TradeDate <= snapshotDate)
                .Select(x => new TradeView
                {
                    Isin = x.Isin,
                    Symbol = x.Symbol,
                    Side = x.Side,
                    Quantity = x.Quantity,
                    Price = x.Price
                })
                .ToListAsync(cancellationToken)
            : await dbContext.TradeEvents
                .AsNoTracking()
                .Where(x => x.AccountId == accountId && x.TradeDate <= snapshotDate)
                .Select(x => new TradeView
                {
                    Isin = x.Isin,
                    Symbol = x.Symbol,
                    Side = x.Side,
                    Quantity = x.Quantity,
                    Price = x.Price
                })
                .ToListAsync(cancellationToken);

        var latestPrices = await dbContext.PriceQuotes
            .AsNoTracking()
            .Where(x => x.PriceDate <= snapshotDate)
            .GroupBy(x => x.Isin)
            .Select(group => group.OrderByDescending(x => x.PriceDate).First())
            .ToDictionaryAsync(x => x.Isin, x => x.PriceUsd, cancellationToken);

        var positions = trades
            .GroupBy(x => new { x.Isin, x.Symbol })
            .Select(group =>
            {
                var signedQuantity = group.Sum(x => x.Side == TradeSide.Buy ? x.Quantity : -x.Quantity);
                var signedCost = group.Sum(x => (x.Side == TradeSide.Buy ? x.Quantity : -x.Quantity) * x.Price);
                var averageCost = signedQuantity == 0m ? 0m : signedCost / signedQuantity;
                var marketPrice = latestPrices.TryGetValue(group.Key.Isin, out var price) ? price : 0m;
                var marketValue = signedQuantity * marketPrice;
                var unrealizedPnL = marketValue - signedQuantity * averageCost;

                return new PortfolioPositionResult
                {
                    Isin = group.Key.Isin,
                    Symbol = group.Key.Symbol,
                    Quantity = Math.Round(signedQuantity, 6),
                    AverageUnitCostUsd = Math.Round(averageCost, 6),
                    MarketPriceUsd = Math.Round(marketPrice, 6),
                    MarketValueUsd = Math.Round(marketValue, 2),
                    UnrealizedPnLUsd = Math.Round(unrealizedPnL, 2)
                };
            })
            .OrderBy(x => x.Symbol)
            .ToList();

        var totalValue = positions.Sum(x => x.MarketValueUsd);

        logger.LogInformation(
            "Generated snapshot for account {AccountId} on {SnapshotDate} with {PositionCount} positions.",
            accountId,
            snapshotDate,
            positions.Count);

        return new PortfolioSnapshotResult
        {
            AccountId = accountId,
            SnapshotDate = snapshotDate,
            Positions = positions,
            TotalMarketValueUsd = Math.Round(totalValue, 2)
        };
    }

    private class TradeView
    {
        public string Isin { get; init; } = string.Empty;
        public string Symbol { get; init; } = string.Empty;
        public TradeSide Side { get; init; }
        public decimal Quantity { get; init; }
        public decimal Price { get; init; }
    }
}
