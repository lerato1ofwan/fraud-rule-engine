using FraudRuleEngine.Transactions.Api.Domain.DTOs;
using FraudRuleEngine.Transactions.Api.Services.Commands;
using FraudRuleEngine.Transactions.Api.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FraudRuleEngine.Transactions.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ISender sender, ILogger<TransactionsController> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new transaction
    /// </summary>
    /// <param name="request">Transaction creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created transaction ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Guid>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTransactionCommand(
            request.AccountId,
            request.Amount,
            request.MerchantId,
            request.Currency,
            request.Timestamp,
            request.ExternalId,
            request.Metadata);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest);
        }

        return CreatedAtAction(
            nameof(GetTransaction),
            new { id = result.Value },
            result.Value);
    }

    /// <summary>
    /// Gets a transaction by ID
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TransactionDto>> GetTransaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetTransactionQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (result.Value is null)
        {
            return Problem(
                detail: "Transaction not found",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result.Value);
    }
}

