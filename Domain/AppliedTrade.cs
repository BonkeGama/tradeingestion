namespace TradeIngestionAssignment.Domain;

public class AppliedTrade
{
    public Guid Id { get; set; }
    public string ExternalRef { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Isin { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public TradeSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateOnly TradeDate { get; set; }
    public DateTime LatestAsOfUtc { get; set; }
    public Guid LatestTradeEventId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public TradeEvent? LatestTradeEvent { get; set; }
}
