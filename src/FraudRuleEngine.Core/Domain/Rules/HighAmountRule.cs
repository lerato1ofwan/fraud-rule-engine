using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Core.Domain.Rules;

/// <summary>
/// Rule that checks if a transaction amount exceeds a threshold.
/// This rule requires no external data, demonstrating that rules can have zero data requirements.
/// </summary>
public class HighAmountRule : IFraudRule
{
    public string RuleName => "HighAmountRule";
    private readonly decimal _threshold;

    public HighAmountRule(decimal threshold = 10000)
    {
        _threshold = threshold;
    }

    public IEnumerable<IRequest<object>> GetDataRequirements(TransactionReceived transaction)
    {
        yield break;
    }

    public Task<FraudRuleEvaluationResult> EvaluateAsync(
        FraudRuleContext context,
        IRuleDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        if (context.Transaction.Amount > _threshold)
        {
            return Task.FromResult(FraudRuleEvaluationResult.RuleTriggered(
                RuleName,
                0.7m,
                $"Transaction amount {context.Transaction.Amount} exceeds threshold {_threshold}"));
        }

        return Task.FromResult(FraudRuleEvaluationResult.RuleNotTriggered(RuleName));
    }
}
