namespace TradeIngestionAssignment.Options;

public class TradeProcessingOptions
{
    public const string SectionName = "TradeProcessing";
    public bool EnableDuplicateCorrectionHandling { get; set; } = true;
}
