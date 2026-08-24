namespace TradeIngestionAssignment.Services;

public class TradeIngestionResult
{
    public Guid TradeEventId { get; set; }
    public Guid? AppliedTradeId { get; set; }
    public TradeIngestionOutcome Outcome { get; set; }
    public string Message { get; set; } = string.Empty;
}
