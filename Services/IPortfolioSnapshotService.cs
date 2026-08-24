namespace TradeIngestionAssignment.Services;

public interface IPortfolioSnapshotService
{
    Task<PortfolioSnapshotResult> GetSnapshotAsync(string accountId, DateOnly snapshotDate, CancellationToken cancellationToken);
}
