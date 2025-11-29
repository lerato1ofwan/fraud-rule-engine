using FraudRuleEngine.Shared.Common;
using MediatR;

namespace FraudRuleEngine.Transactions.Api.Services.Commands;

public record CreateTransactionCommand(
    Guid AccountId,
    decimal Amount,
    Guid MerchantId,
    string Currency,
    DateTime Timestamp,
    string ExternalId,
    Dictionary<string, string>? Metadata) : IRequest<Result<Guid>>;

