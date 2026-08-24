using Microsoft.AspNetCore.Mvc;
using TradeIngestionAssignment.Contracts;
using TradeIngestionAssignment.Services;

namespace TradeIngestionAssignment.Controllers;

[ApiController]
[Route("api/snapshots")]
public class SnapshotsController(IPortfolioSnapshotService portfolioSnapshotService) : ControllerBase
{
    [HttpGet("{accountId}")]
    [ProducesResponseType(typeof(PortfolioSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSnapshot([FromRoute] string accountId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return ValidationProblem("accountId is required.");
        }

        var snapshot = await portfolioSnapshotService.GetSnapshotAsync(accountId.Trim(), date, cancellationToken);

        return Ok(new PortfolioSnapshotResponse
        {
            AccountId = snapshot.AccountId,
            SnapshotDate = snapshot.SnapshotDate,
            TotalMarketValueUsd = snapshot.TotalMarketValueUsd,
            Positions = snapshot.Positions.Select(x => new PortfolioPositionResponse
            {
                Isin = x.Isin,
                Symbol = x.Symbol,
                Quantity = x.Quantity,
                AverageUnitCostUsd = x.AverageUnitCostUsd,
                MarketPriceUsd = x.MarketPriceUsd,
                MarketValueUsd = x.MarketValueUsd,
                UnrealizedPnLUsd = x.UnrealizedPnLUsd
            }).ToList()
        });
    }
}
