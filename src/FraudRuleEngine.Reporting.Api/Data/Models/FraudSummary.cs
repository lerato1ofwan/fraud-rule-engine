namespace FraudRuleEngine.Reporting.Api.Data.Models;

public class FraudSummary
{
    public int Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid FraudCheckId { get; set; }
    public bool IsFlagged { get; set; }
    public decimal OverallRiskScore { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

