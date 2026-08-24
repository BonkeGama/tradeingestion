using TradeIngestionAssignment.Domain;

namespace TradeIngestionAssignment.Services;

public class TradeSubmissionCommand
{
    public string ExternalRef { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Isin { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public TradeSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateOnly TradeDate { get; set; }
    public DateTime AsOfUtc { get; set; }
}
