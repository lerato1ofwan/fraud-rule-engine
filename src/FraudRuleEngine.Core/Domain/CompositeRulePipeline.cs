using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.Specifications;
using FraudRuleEngine.Core.Domain.ValueObjects;

namespace FraudRuleEngine.Core.Domain;

/// <summary>
/// Composes the evaluation of multiple fraud rules in a pipeline.
/// Supports type-safe data resolution through IRuleDataContext.
/// </summary>
public class CompositeRulePipeline
{
    private readonly List<IFraudRule> _rules;
    private readonly ISpecification<FraudRuleEvaluationResult>? _specification;

    public CompositeRulePipeline(List<IFraudRule> rules, ISpecification<FraudRuleEvaluationResult>? specification = null)
    {
        _rules = rules;
        _specification = specification;
    }

    /// <summary>
    /// Gets all data requirements from all rules in the pipeline.
    /// Used by the evaluation service to pre-fetch all required data.
    /// </summary>
    public IEnumerable<IRequest<object>> GetAllDataRequirements(Shared.Contracts.TransactionReceived transaction)
    {
        return _rules.SelectMany(rule => rule.GetDataRequirements(transaction));
    }

    public async Task<List<FraudRuleEvaluationResult>> EvaluateAllAsync(
        FraudRuleContext context,
        IRuleDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        var results = new List<FraudRuleEvaluationResult>();

        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(context, dataContext, cancellationToken);

            if (_specification != null && _specification.IsSatisfiedBy(result))
            {
                results.Add(result);
            }
            else if (_specification == null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    public async Task<FraudCheckResult> EvaluateAsync(
        FraudRuleContext context,
        IRuleDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        var results = await EvaluateAllAsync(context, dataContext, cancellationToken);
        var triggeredResults = results.Where(r => r.Triggered).ToList();
        var overallRiskScore = triggeredResults.Any()
            ? triggeredResults.Average(r => r.RiskScore)
            : 0m;
        var IsFlagged = overallRiskScore >= 0.5m;

        return new FraudCheckResult
        {
            IsFlagged = IsFlagged,
            OverallRiskScore = overallRiskScore,
            RuleResults = results
        };
    }
}