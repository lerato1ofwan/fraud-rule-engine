using FraudRuleEngine.Shared.Common;
using FraudRuleEngine.Transactions.Api.Domain.DTOs;
using MediatR;

namespace FraudRuleEngine.Transactions.Api.Services.Queries;

public record GetTransactionQuery(Guid TransactionId) : IRequest<Result<TransactionDto?>>;

