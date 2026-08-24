using Microsoft.AspNetCore.Mvc;
using TradeIngestionAssignment.Contracts;
using TradeIngestionAssignment.Domain;
using TradeIngestionAssignment.Services;

namespace TradeIngestionAssignment.Controllers;

[ApiController]
[Route("api/trades")]
public class TradesController(ITradeIngestionService tradeIngestionService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TradeSubmissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitTrade([FromBody] TradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TradeSide>(request.Side, true, out var side))
        {
            return ValidationProblem("side must be BUY or SELL");
        }

        if (request.AsOfUtc.Kind != DateTimeKind.Utc)
        {
            return ValidationProblem("as_of must be provided in UTC.");
        }

        var result = await tradeIngestionService.IngestAsync(new TradeSubmissionCommand
        {
            ExternalRef = request.ExternalRef.Trim(),
            AccountId = request.AccountId.Trim(),
            Isin = request.Isin.Trim().ToUpperInvariant(),
            Symbol = request.Symbol.Trim().ToUpperInvariant(),
            Side = side,
            Quantity = request.Quantity,
            Price = request.Price,
            TradeDate = request.TradeDate,
            AsOfUtc = request.AsOfUtc
        }, cancellationToken);

        var response = new TradeSubmissionResponse
        {
            TradeEventId = result.TradeEventId,
            AppliedTradeId = result.AppliedTradeId,
            Outcome = result.Outcome.ToString(),
            Message = result.Message
        };

        return Created($"/api/trades/{response.TradeEventId}", response);
    }
}
