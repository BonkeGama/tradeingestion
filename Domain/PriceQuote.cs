namespace TradeIngestionAssignment.Domain;

public class PriceQuote
{
    public Guid Id { get; set; }
    public string Isin { get; set; } = string.Empty;
    public DateOnly PriceDate { get; set; }
    public decimal PriceUsd { get; set; }
    public string Currency { get; set; } = "USD";
}
