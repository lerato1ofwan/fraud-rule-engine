namespace FraudRuleEngine.Transactions.Api.Domain.DTOs;

public class TransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public Guid MerchantId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

