namespace TradeIngestionAssignment.Services;

public enum TradeIngestionOutcome
{
    AcceptedWithoutProtection = 1,
    AppliedNew = 2,
    DuplicateIgnored = 3,
    CorrectionApplied = 4
}
