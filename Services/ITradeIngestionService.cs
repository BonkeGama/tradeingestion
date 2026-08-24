namespace TradeIngestionAssignment.Services;

public interface ITradeIngestionService
{
    Task<TradeIngestionResult> IngestAsync(TradeSubmissionCommand command, CancellationToken cancellationToken);
}
