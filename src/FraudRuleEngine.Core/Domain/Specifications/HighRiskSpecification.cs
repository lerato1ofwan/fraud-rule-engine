using FraudRuleEngine.Core.Domain.ValueObjects;

namespace FraudRuleEngine.Core.Domain.Specifications;

public class HighRiskSpecification : ISpecification<FraudRuleEvaluationResult>
{
    private readonly decimal _riskThreshold;

    public HighRiskSpecification(decimal riskThreshold = 0.5m)
    {
        _riskThreshold = riskThreshold;
    }

    public bool IsSatisfiedBy(FraudRuleEvaluationResult candidate)
    {
        return candidate.Triggered && candidate.RiskScore >= _riskThreshold;
    }
}
