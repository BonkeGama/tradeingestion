using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradeIngestionAssignment.Data;
using TradeIngestionAssignment.Domain;
using TradeIngestionAssignment.Options;

namespace TradeIngestionAssignment.Services;

public class TradeIngestionService(
    TradeDbContext dbContext,
    IOptions<TradeProcessingOptions> options,
    ILogger<TradeIngestionService> logger) : ITradeIngestionService
{
    private readonly TradeProcessingOptions _options = options.Value;

    public async Task<TradeIngestionResult> IngestAsync(TradeSubmissionCommand command, CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["external_ref"] = command.ExternalRef,
            ["account_id"] = command.AccountId,
            ["as_of"] = command.AsOfUtc
        });

        var nowUtc = DateTime.UtcNow;
        var tradeEvent = new TradeEvent
        {
            Id = Guid.NewGuid(),
            ExternalRef = command.ExternalRef,
            AccountId = command.AccountId,
            Isin = command.Isin,
            Symbol = command.Symbol,
            Side = command.Side,
            Quantity = command.Quantity,
            Price = command.Price,
            TradeDate = command.TradeDate,
            AsOfUtc = command.AsOfUtc,
            ReceivedAtUtc = nowUtc
        };

        var supportsTransactions = dbContext.Database.IsRelational();
        await using var transaction = supportsTransactions
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        dbContext.TradeEvents.Add(tradeEvent);

        if (!_options.EnableDuplicateCorrectionHandling)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            logger.LogInformation(
                "Trade event persisted with duplicate/correction handling disabled.");

            return new TradeIngestionResult
            {
                TradeEventId = tradeEvent.Id,
                Outcome = TradeIngestionOutcome.AcceptedWithoutProtection,
                Message = "Trade accepted without duplicate/correction handling."
            };
        }

        var existingApplied = await dbContext.AppliedTrades
            .SingleOrDefaultAsync(x => x.ExternalRef == command.ExternalRef, cancellationToken);

        if (existingApplied is null)
        {
            var appliedTrade = new AppliedTrade
            {
                Id = Guid.NewGuid(),
                ExternalRef = command.ExternalRef,
                AccountId = command.AccountId,
                Isin = command.Isin,
                Symbol = command.Symbol,
                Side = command.Side,
                Quantity = command.Quantity,
                Price = command.Price,
                TradeDate = command.TradeDate,
                LatestAsOfUtc = command.AsOfUtc,
                LatestTradeEventId = tradeEvent.Id,
                UpdatedAtUtc = nowUtc
            };

            dbContext.AppliedTrades.Add(appliedTrade);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            logger.LogInformation("Trade applied as new external reference.");

            return new TradeIngestionResult
            {
                TradeEventId = tradeEvent.Id,
                AppliedTradeId = appliedTrade.Id,
                Outcome = TradeIngestionOutcome.AppliedNew,
                Message = "Trade applied as new reference."
            };
        }

        if (command.AsOfUtc <= existingApplied.LatestAsOfUtc)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            logger.LogInformation(
                "Trade event identified as duplicate or stale correction and ignored for projection.");

            return new TradeIngestionResult
            {
                TradeEventId = tradeEvent.Id,
                AppliedTradeId = existingApplied.Id,
                Outcome = TradeIngestionOutcome.DuplicateIgnored,
                Message = "Duplicate/stale event kept in audit trail and not applied."
            };
        }

        existingApplied.AccountId = command.AccountId;
        existingApplied.Isin = command.Isin;
        existingApplied.Symbol = command.Symbol;
        existingApplied.Side = command.Side;
        existingApplied.Quantity = command.Quantity;
        existingApplied.Price = command.Price;
        existingApplied.TradeDate = command.TradeDate;
        existingApplied.LatestAsOfUtc = command.AsOfUtc;
        existingApplied.LatestTradeEventId = tradeEvent.Id;
        existingApplied.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        logger.LogInformation("Trade correction applied to latest projection.");

        return new TradeIngestionResult
        {
            TradeEventId = tradeEvent.Id,
            AppliedTradeId = existingApplied.Id,
            Outcome = TradeIngestionOutcome.CorrectionApplied,
            Message = "Trade correction applied from later as_of timestamp."
        };
    }
}
