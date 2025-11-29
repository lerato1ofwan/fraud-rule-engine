using FraudRuleEngine.Shared.Common;
using FraudRuleEngine.Transactions.Api.Data.Repositories;
using FraudRuleEngine.Transactions.Api.Domain.Entities;
using MediatR;
using FraudRuleEngine.Shared.Metrics;

namespace FraudRuleEngine.Transactions.Api.Services.Commands;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Result<Guid>>
{
    private readonly ITransactionRepository _repository;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository repository,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var existing = await _repository.GetByExternalIdAsync(request.ExternalId, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Transaction with ExternalId {ExternalId} already exists", request.ExternalId);
                return Result<Guid>.Success(existing.TransactionId);
            }

            var transaction = Transaction.Create(
                request.AccountId,
                request.Amount,
                request.MerchantId,
                request.Currency,
                request.Timestamp,
                request.ExternalId,
                request.Metadata ?? new Dictionary<string, string>());

            await _repository.AddAsync(transaction, cancellationToken);

            // Increment the metric after successfully adding the transaction
            FraudMetrics.TransactionsReceivedTotal.Add(1, new KeyValuePair<string, object?>("status", "new"));
            _logger.LogInformation("Incremented fraud_transactions_received_total metric for transaction {TransactionId}", transaction.TransactionId);

            return Result<Guid>.Success(transaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create transaction with ExternalId {ExternalId}", request.ExternalId);
            return Result<Guid>.Failure("Unable to create the transaction at this time. Please retry.");
        }
    }
}
