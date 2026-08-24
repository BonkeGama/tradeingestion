namespace TradeIngestionAssignment.Domain;

public class Instrument
{
    public Guid Id { get; set; }
    public string Isin { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
