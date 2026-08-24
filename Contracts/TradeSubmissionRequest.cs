using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TradeIngestionAssignment.Contracts;

public class TradeSubmissionRequest
{
    [Required]
    [JsonPropertyName("external_ref")]
    public string ExternalRef { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("account_id")]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("isin")]
    public string Isin { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    [RegularExpression("BUY|SELL")]
    [JsonPropertyName("side")]
    public string Side { get; set; } = string.Empty;

    [Range(0.000001, double.MaxValue)]
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [Range(0.000001, double.MaxValue)]
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("trade_date")]
    public DateOnly TradeDate { get; set; }

    [JsonPropertyName("as_of")]
    public DateTime AsOfUtc { get; set; }
}
