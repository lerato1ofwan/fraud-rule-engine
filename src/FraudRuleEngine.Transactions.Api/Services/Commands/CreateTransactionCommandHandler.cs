using System.Collections.Generic;
using System;
using FraudRuleEngine.Shared.Common;
using FraudRuleEngine.Transactions.Api.Data.Repositories;
using FraudRuleEngine.Transactions.Api.Data.UnitOfWork;
using FraudRuleEngine.Transactions.Api.Domain.Entities;
using FraudRuleEngine.Transactions.Api.Services.Messaging;
using MediatR;

namespace FraudRuleEngine.Transactions.Api.Services.Commands;

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Result<Guid>>
{
    private readonly ITransactionRepository _repository;
    private readonly IOutboxService _outboxService;
    private readonly ITransactionUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository repository,
        IOutboxService outboxService,
        ITransactionUnitOfWork unitOfWork,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _repository = repository;
        _outboxService = outboxService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteAsync<Guid>(async ct =>
        {
            try
            {
                var existing = await _repository.GetByExternalIdAsync(request.ExternalId, ct);
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

                await _repository.AddAsync(transaction, ct);

                return Result<Guid>.Success(transaction.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest transaction with ExternalId {ExternalId}", request.ExternalId);
                return Result<Guid>.Failure("Unable to ingest the transaction at this time. Please retry.");
            }
        }, cancellationToken);
    }
}
