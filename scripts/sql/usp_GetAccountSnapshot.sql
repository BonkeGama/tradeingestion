/*
SQL Server artifact for account/date valuation using latest correction per external reference.
Assumptions:
- TradeEvents is append-only.
- Latest event per external_ref (as_of then received_at) is effective.
- Snapshot cutoff includes rows with as_of < next day UTC.
*/

IF OBJECT_ID('dbo.usp_GetAccountSnapshot', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_GetAccountSnapshot;
GO

CREATE PROCEDURE dbo.usp_GetAccountSnapshot
	@AccountId NVARCHAR(64),
	@SnapshotDate DATE
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @SnapshotUpperBoundUtc DATETIME2(7) = DATEADD(DAY, 1, CAST(@SnapshotDate AS DATETIME2(7)));

	;WITH RankedEvents AS
	(
		SELECT
			te.ExternalRef,
			te.AccountId,
			te.Isin,
			te.Symbol,
			te.Side,
			te.Quantity,
			te.Price,
			te.TradeDate,
			te.AsOfUtc,
			te.ReceivedAtUtc,
			ROW_NUMBER() OVER
			(
				PARTITION BY te.ExternalRef
				ORDER BY te.AsOfUtc DESC, te.ReceivedAtUtc DESC, te.Id DESC
			) AS rn
		FROM dbo.TradeEvents te
		WHERE te.AccountId = @AccountId
		  AND te.TradeDate <= @SnapshotDate
		  AND te.AsOfUtc < @SnapshotUpperBoundUtc
	),
	EffectiveTrades AS
	(
		SELECT
			re.Isin,
			re.Symbol,
			CASE WHEN re.Side = 1 THEN re.Quantity ELSE -re.Quantity END AS SignedQuantity,
			CASE WHEN re.Side = 1 THEN re.Quantity * re.Price ELSE -re.Quantity * re.Price END AS SignedCost
		FROM RankedEvents re
		WHERE re.rn = 1
	),
	PositionAgg AS
	(
		SELECT
			et.Isin,
			MAX(et.Symbol) AS Symbol,
			SUM(et.SignedQuantity) AS Quantity,
			SUM(et.SignedCost) AS CostBasis
		FROM EffectiveTrades et
		GROUP BY et.Isin
	),
	RankedPrices AS
	(
		SELECT
			pq.Isin,
			pq.PriceUsd,
			ROW_NUMBER() OVER (PARTITION BY pq.Isin ORDER BY pq.PriceDate DESC, pq.Id DESC) AS rn
		FROM dbo.PriceQuotes pq
		WHERE pq.PriceDate <= @SnapshotDate
	),
	LatestPrices AS
	(
		SELECT rp.Isin, rp.PriceUsd
		FROM RankedPrices rp
		WHERE rp.rn = 1
	)
	SELECT
		pa.Isin,
		pa.Symbol,
		CAST(pa.Quantity AS DECIMAL(18, 6)) AS Quantity,
		CAST(CASE WHEN pa.Quantity = 0 THEN 0 ELSE pa.CostBasis / pa.Quantity END AS DECIMAL(18, 6)) AS UnitCostUsd,
		CAST(ISNULL(lp.PriceUsd, 0) AS DECIMAL(18, 6)) AS MarketPriceUsd,
		CAST(pa.Quantity * ISNULL(lp.PriceUsd, 0) AS DECIMAL(18, 2)) AS MarketValueUsd,
		CAST((pa.Quantity * ISNULL(lp.PriceUsd, 0)) - pa.CostBasis AS DECIMAL(18, 2)) AS UnrealizedPnLUsd,
		CAST(SUM(pa.Quantity * ISNULL(lp.PriceUsd, 0)) OVER() AS DECIMAL(18, 2)) AS AccountTotalMarketValueUsd
	FROM PositionAgg pa
	LEFT JOIN LatestPrices lp ON lp.Isin = pa.Isin
	ORDER BY pa.Symbol;

	SELECT
		@AccountId AS AccountId,
		@SnapshotDate AS SnapshotDate,
		CAST(SUM(pa.Quantity * ISNULL(lp.PriceUsd, 0)) AS DECIMAL(18, 2)) AS TotalMarketValueUsd,
		CAST(SUM(pa.CostBasis) AS DECIMAL(18, 2)) AS TotalCostBasisUsd,
		CAST(SUM((pa.Quantity * ISNULL(lp.PriceUsd, 0)) - pa.CostBasis) AS DECIMAL(18, 2)) AS TotalUnrealizedPnLUsd
	FROM PositionAgg pa
	LEFT JOIN LatestPrices lp ON lp.Isin = pa.Isin;
END;
GO

/* Supporting indexes for this artifact's access paths */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeEvents_Account_TradeDate_AsOf_ExternalRef' AND object_id = OBJECT_ID('dbo.TradeEvents'))
BEGIN
	CREATE INDEX IX_TradeEvents_Account_TradeDate_AsOf_ExternalRef
	ON dbo.TradeEvents (AccountId, TradeDate, AsOfUtc, ExternalRef)
	INCLUDE (Side, Quantity, Price, Symbol, ReceivedAtUtc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PriceQuotes_Isin_PriceDate_Desc' AND object_id = OBJECT_ID('dbo.PriceQuotes'))
BEGIN
	CREATE INDEX IX_PriceQuotes_Isin_PriceDate_Desc
	ON dbo.PriceQuotes (Isin, PriceDate DESC)
	INCLUDE (PriceUsd);
END;
GO
