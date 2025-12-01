using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Core.Tests.Helpers;

public static class TestDataBuilder
{
    public static TransactionReceived CreateTransaction(
        Guid? accountId = null,
        decimal? amount = null,
        string? currency = null,
        Guid? merchantId = null,
        DateTime? timestamp = null,
        Dictionary<string, string>? metadata = null)
    {
        return new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = accountId ?? Guid.NewGuid(),
            Amount = amount ?? 1000m,
            Currency = currency ?? "ZAR",
            MerchantId = merchantId ?? Guid.NewGuid(),
            Timestamp = timestamp ?? DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, string> { { "Country", "RSA" } }
        };
    }

    public static TransactionReceived CreateHighAmountTransaction(decimal amount = 15000m)
    {
        return CreateTransaction(amount: amount);
    }

    public static TransactionReceived CreateForeignCountryTransaction(string country = "USA")
    {
        return CreateTransaction(metadata: new Dictionary<string, string> { { "Country", country } });
    }
}

