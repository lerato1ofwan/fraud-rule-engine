namespace FraudRuleEngine.Core.Domain.ValueObjects;

public class FraudRuleEvaluationResult
{
    public string RuleName { get; init; } = string.Empty;
    public bool Triggered { get; init; }
    public decimal RiskScore { get; init; }
    public string Reason { get; init; } = string.Empty;

    public static FraudRuleEvaluationResult RuleNotTriggered(string ruleName) => new()
    {
        RuleName = ruleName,
        Triggered = false,
        RiskScore = 0,
        Reason = "Rule did not trigger"
    };

    public static FraudRuleEvaluationResult RuleTriggered(string ruleName, decimal riskScore, string reason) => new()
    {
        RuleName = ruleName,
        Triggered = true,
        RiskScore = riskScore,
        Reason = reason
    };
}
