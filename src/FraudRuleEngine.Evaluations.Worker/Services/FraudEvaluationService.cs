using FraudRuleEngine.Core.Domain;
using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Evaluations.Worker.Data.Models;
using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Evaluations.Worker.Services;

/// <summary>
/// Triggers and kicks off the fraud rules evaluation on a received transaction.
/// </summary>
public class FraudEvaluationService : IFraudEvaluationService
{
    private readonly CompositeRulePipeline _rulePipeline;
    private readonly IFraudCheckRepository _repository;
    private readonly IRuleDataContext _dataContext;

    public FraudEvaluationService(
        CompositeRulePipeline rulePipeline,
        IFraudCheckRepository repository,
        IRuleDataContext dataContext)
    {
        _rulePipeline = rulePipeline;
        _repository = repository;
        _dataContext = dataContext;
    }

    public async Task<FraudCheck> EvaluateAsync(
        TransactionReceived transaction,
        CancellationToken cancellationToken = default)
    {
        var context = new FraudRuleContext
        {
            Transaction = transaction
        };

        // Evaluate all rules with the data context
        // Rules will use the data context to resolve their specific data needs lazily
        // The data context dispatches requests to appropriate handlers automatically
        var result = await _rulePipeline.EvaluateAsync(context, _dataContext, cancellationToken);

        // Save the fraud check results
        var ruleResults = result.RuleResults.Select(r => FraudRuleResult.Create(
            r.RuleName,
            r.Triggered,
            r.RiskScore,
            r.Reason)).ToList();

        var fraudCheck = FraudCheck.Create(
            transaction.TransactionId,
            transaction.AccountId,
            result.IsFlagged,
            result.OverallRiskScore,
            ruleResults);

        await _repository.AddAsync(fraudCheck, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return fraudCheck;
    }
}
