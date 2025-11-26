using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Core.Domain.ValueObjects;

public class FraudRuleContext
{
    public TransactionReceived Transaction { get; init; } = null!;
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}
