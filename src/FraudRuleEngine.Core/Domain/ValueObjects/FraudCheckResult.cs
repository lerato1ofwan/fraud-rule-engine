namespace FraudRuleEngine.Core.Domain.ValueObjects
{
    public class FraudCheckResult
    {
        public bool IsFlagged { get; set; }
        public decimal OverallRiskScore { get; set; }
        public List<FraudRuleEvaluationResult> RuleResults { get; set; } = new();
    }

}
