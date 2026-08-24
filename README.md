# Trade Ingestion Assignment

## Prerequisites
- .NET SDK 10 (project target in this workspace)
- SQL Server localhost

## Run
1. Update connection string in `appsettings.Development.json` if needed.
2. Start the API:
   - `dotnet run --project TradeIngestionAssignment.csproj`
3. Execute the sample flow in `TradeIngestionAssignment.http`.

The API initializes schema at startup and seeds:
- Instrument: AAPL (`US0378331005`)
- Price: 186.00 USD on 2025-03-01

## Endpoints
- `POST /api/trades`
- `GET /api/snapshots/{accountId}?date=YYYY-MM-DD`

## SQL Deep Dive Artifact
- `scripts/sql/usp_GetAccountSnapshot.sql`

## Automated Test
Run:
- `dotnet test`
