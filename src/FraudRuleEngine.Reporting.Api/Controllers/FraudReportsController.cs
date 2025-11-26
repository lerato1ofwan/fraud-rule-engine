using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using FraudRuleEngine.Reporting.Api.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FraudRuleEngine.Reporting.Api.Controllers;

[ApiController]
[Route("api/fraud-reports")]
[Produces("application/json")]
public class FraudReportsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<FraudReportsController> _logger;

    public FraudReportsController(ISender sender, ILogger<FraudReportsController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Gets fraud summary for a transaction
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fraud summary</returns>
    [HttpGet("summary/{transactionId:guid}")]
    [ProducesResponseType(typeof(FraudSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FraudSummaryDto>> GetFraudSummary(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var query = new GetFraudSummaryQuery(transactionId);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (result.Value == null)
        {
            return Problem(
                detail: "Fraud summary not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets daily fraud statistics
    /// </summary>
    /// <param name="date">Date for statistics (defaults to today)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Daily statistics</returns>
    [HttpGet("stats/daily")]
    [ProducesResponseType(typeof(DailyStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyStatsDto>> GetDailyStats(
        [FromQuery] DateTime? date,
        CancellationToken cancellationToken)
    {
        var query = new GetDailyStatsQuery(date ?? DateTime.UtcNow.Date);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets top triggered fraud rules
    /// </summary>
    /// <param name="top">Number of top rules to return (default: 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top rules</returns>
    [HttpGet("rules/top")]
    [ProducesResponseType(typeof(List<TopRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TopRuleDto>>> GetTopRules(
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTopRulesQuery(top);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value);
    }
}

