namespace TradeIngestionAssignment.Services;

public class PortfolioSnapshotResult
{
    public string AccountId { get; set; } = string.Empty;
    public DateOnly SnapshotDate { get; set; }
    public decimal TotalMarketValueUsd { get; set; }
    public List<PortfolioPositionResult> Positions { get; set; } = [];
}

public class PortfolioPositionResult
{
    public string Isin { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageUnitCostUsd { get; set; }
    public decimal MarketPriceUsd { get; set; }
    public decimal MarketValueUsd { get; set; }
    public decimal UnrealizedPnLUsd { get; set; }
}
