using FraudRuleEngine.Shared.Common;
using FraudRuleEngine.Transactions.Api.Data.Repositories;
using FraudRuleEngine.Transactions.Api.Domain.DTOs;
using MediatR;

namespace FraudRuleEngine.Transactions.Api.Services.Queries;

public class GetTransactionQueryHandler : IRequestHandler<GetTransactionQuery, Result<TransactionDto?>>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TransactionDto?>> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _repository.GetByIdAsync(request.TransactionId, cancellationToken);
        
        if (transaction == null)
        {
            return Result<TransactionDto?>.Success(null);
        }

        var dto = new TransactionDto
        {
            TransactionId = transaction.TransactionId,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            MerchantId = transaction.MerchantId,
            Currency = transaction.Currency,
            Timestamp = transaction.Timestamp,
            ExternalId = transaction.ExternalId,
            Metadata = transaction.Metadata.ToDictionary(),
            CreatedAt = transaction.CreatedAt
        };

        return Result<TransactionDto?>.Success(dto);
    }
}

