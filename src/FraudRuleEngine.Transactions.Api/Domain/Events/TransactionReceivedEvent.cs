using FraudRuleEngine.Shared.Events;

namespace FraudRuleEngine.Transactions.Api.Domain.Events;

public record TransactionReceivedEvent(
    Guid TransactionId,
    Guid AccountId,
    decimal Amount,
    Guid MerchantId,
    string Currency,
    DateTime Timestamp,
    Dictionary<string, string> Metadata) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

