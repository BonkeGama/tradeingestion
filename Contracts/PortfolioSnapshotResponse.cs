using System.Text.Json.Serialization;

namespace TradeIngestionAssignment.Contracts;

public class PortfolioSnapshotResponse
{
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [JsonPropertyName("snapshot_date")]
    public DateOnly SnapshotDate { get; set; }

    [JsonPropertyName("total_market_value_usd")]
    public decimal TotalMarketValueUsd { get; set; }

    [JsonPropertyName("positions")]
    public List<PortfolioPositionResponse> Positions { get; set; } = [];
}

public class PortfolioPositionResponse
{
    [JsonPropertyName("isin")]
    public string Isin { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("average_unit_cost_usd")]
    public decimal AverageUnitCostUsd { get; set; }

    [JsonPropertyName("market_price_usd")]
    public decimal MarketPriceUsd { get; set; }

    [JsonPropertyName("market_value_usd")]
    public decimal MarketValueUsd { get; set; }

    [JsonPropertyName("unrealized_pnl_usd")]
    public decimal UnrealizedPnLUsd { get; set; }
}
