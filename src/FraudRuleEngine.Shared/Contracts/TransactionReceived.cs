namespace FraudRuleEngine.Shared.Contracts;

public record TransactionReceived
{
    public Guid TransactionId { get; init; }
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public Guid MerchantId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

