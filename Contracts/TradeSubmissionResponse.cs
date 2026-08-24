using System.Text.Json.Serialization;

namespace TradeIngestionAssignment.Contracts;

public class TradeSubmissionResponse
{
    [JsonPropertyName("trade_event_id")]
    public Guid TradeEventId { get; set; }

    [JsonPropertyName("applied_trade_id")]
    public Guid? AppliedTradeId { get; set; }

    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
