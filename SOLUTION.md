# SOLUTION

## Design Summary
- **Audit model:** every submission is written to `TradeEvents` (append-only).
- **Applied projection:** `AppliedTrades` holds latest effective row by `external_ref`.
- **Duplicate rule:** same `external_ref` with `as_of` <= applied `as_of` is treated as duplicate/stale and not re-applied.
- **Correction rule:** same `external_ref` with later `as_of` updates the applied projection.
- **Snapshot rule:** account/date snapshot aggregates signed quantities (BUY positive, SELL negative), average unit cost, market value, and unrealized P/L in USD.

## Assumptions
- Base/reporting currency is USD.
- Prices are looked up by latest `PriceDate <= snapshot date`.
- Cost basis uses a simplified signed average cost per instrument/account.
- Monetary values in API responses are rounded to 2 decimals; internal storage keeps 6-decimal precision.

## Safe Rollout / Disablement
- Config option: `TradeProcessing:EnableDuplicateCorrectionHandling`.
- **Enabled (default):** snapshot uses `AppliedTrades` projection, protecting against duplicate re-sends and applying later corrections.
- **Disabled:** ingestion still stores audit events, but snapshot reads raw `TradeEvents` (legacy-like behavior, duplicates can impact totals).
- This allows gradual rollout with a reversible switch during adoption.

## Structured Logging
- JSON console logging with scopes.
- Ingestion scope includes `external_ref`, `account_id`, and `as_of`.
- Outcome logs: new apply, duplicate ignored, correction applied, handling disabled.

## Demo Flow
1. Submit initial trade `TRX-1001` (BUY 120 @ 185.40, as_of `2025-03-01T10:15:00Z`).
2. Re-send exact duplicate (`TRX-1001`, same as_of) -> stored in audit, not re-applied.
3. Submit correction (`TRX-1001`, BUY 100 @ 185.40, later as_of) -> applied projection updated.
4. Query snapshot for account/date -> quantity/value reflects corrected quantity, not duplicate double count.

## SQL Artifact (Task 2)
- Stored procedure: `dbo.usp_GetAccountSnapshot` in `scripts/sql/usp_GetAccountSnapshot.sql`.
- Uses CTEs + window functions for latest-version selection and latest pricing.
- Returns instrument-level summary and account-level summary.
- Includes recommended supporting indexes for SARGable account/date/as_of lookups.

## Performance & Maintainability at Scale
- Keep predicates SARGable (`AccountId`, `TradeDate`, `AsOfUtc` typed filters).
- Composite indexes align to filter and partition keys used by ranking CTEs.
- Partition strategy: range partition `TradeEvents` by `TradeDate` (monthly/quarterly) with archival to cheaper storage.
- Consider precomputed daily snapshot tables/materialization for very high read volume.
- Maintain stats and monitor execution plans for regression after data growth.

## .NET Framework 4.8 Integration Considerations
- Use a compatible data-access layer boundary (repository/service contracts) so legacy callers can consume via HTTP or shared DB contracts.
- Prefer out-of-process structured logging sink (e.g., Serilog sink/ETW/EventSource bridge) if modern logging abstractions are limited in older hosting stacks.
- Keep dependencies isolated: modern API service can run side-by-side while .NET Framework 4.8 apps integrate through REST or messaging.
