namespace FraudRuleEngine.Reporting.Api.Domain.DTOs;

public class FraudSummaryDto
{
    public Guid TransactionId { get; set; }
    public Guid FraudCheckId { get; set; }
    public bool IsFlagged { get; set; }
    public decimal OverallRiskScore { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

