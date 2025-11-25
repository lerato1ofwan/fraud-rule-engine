namespace FraudRuleEngine.Transactions.Api.Domain.ValueObjects;

public class TransactionMetadata
{
    public Dictionary<string, string> Data { get; private set; } = new();

    private TransactionMetadata() { } // EF Core

    private TransactionMetadata(Dictionary<string, string> data)
    {
        Data = data;
    }

    public static TransactionMetadata Create(Dictionary<string, string> data)
    {
        return new TransactionMetadata(data ?? new Dictionary<string, string>());
    }

    public Dictionary<string, string> ToDictionary() => Data;
}

