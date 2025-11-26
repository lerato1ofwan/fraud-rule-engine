namespace FraudRuleEngine.Core.Domain.ValueObjects;

public class FraudRuleResult
{
    public string RuleName { get; private set; } = string.Empty;
    public bool Triggered { get; private set; }
    public decimal RiskScore { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    private FraudRuleResult() { }

    private FraudRuleResult(string ruleName, bool triggered, decimal riskScore, string reason)
    {
        RuleName = ruleName;
        Triggered = triggered;
        RiskScore = riskScore;
        Reason = reason;
    }

    public static FraudRuleResult Create(string ruleName, bool triggered, decimal riskScore, string reason)
    {
        return new FraudRuleResult(ruleName, triggered, riskScore, reason);
    }
}
