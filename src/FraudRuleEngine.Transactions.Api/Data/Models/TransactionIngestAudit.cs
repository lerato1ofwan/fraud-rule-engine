namespace FraudRuleEngine.Transactions.Api.Data;

public class TransactionIngestAudit
{
    public int Id { get; set; }
    public Guid TransactionId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public DateTime IngestedAt { get; set; }
}

