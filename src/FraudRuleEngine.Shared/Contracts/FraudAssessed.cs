namespace FraudRuleEngine.Shared.Contracts;

public record FraudAssessed
{
    public Guid FraudCheckId { get; init; }
    public Guid TransactionId { get; init; }
    public bool IsFlagged { get; init; }
    public decimal OverallRiskScore { get; init; }
    public List<RuleResult> RuleResults { get; init; } = new();
}

public record RuleResult
{
    public string RuleName { get; init; } = string.Empty;
    public bool Triggered { get; init; }
    public decimal RiskScore { get; init; }
    public string Reason { get; init; } = string.Empty;
}

