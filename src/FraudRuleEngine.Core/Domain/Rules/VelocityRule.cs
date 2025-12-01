using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Core.Domain.Rules;

/// <summary>
/// Rule that checks if an account has an excessive number of transactions within a time window.
/// Demonstrates the use of type-safe data requests instead of magic strings.
/// </summary>
public class VelocityRule : IFraudRule
{
    public string RuleName => "VelocityRule";
    private readonly int _maxTransactionsPerHour;
    private readonly TimeSpan _timeWindow;

    public VelocityRule(int maxTransactionsPerHour = 10, TimeSpan? timeWindow = null)
    {
        _maxTransactionsPerHour = maxTransactionsPerHour;
        _timeWindow = timeWindow ?? TimeSpan.FromHours(1);
    }

    public IEnumerable<IRequest<object>> GetDataRequirements(TransactionReceived transaction)
    {
        // Declare what data this rule needs
        var request = RecentTransactionCountRequest.FromTransaction(transaction, _timeWindow);
        yield return RequestWrapper.Wrap(request);
    }

    public async Task<FraudRuleEvaluationResult> EvaluateAsync(
        FraudRuleContext context,
        IRuleDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        var request = RecentTransactionCountRequest.FromTransaction(context.Transaction, _timeWindow);
        var recentTransactions = await dataContext.ResolveAsync<RecentTransactionCountRequest, int>(
            request,
            cancellationToken);

        if (recentTransactions >= _maxTransactionsPerHour)
        {
            return FraudRuleEvaluationResult.RuleTriggered(
                RuleName,
                0.8m,
                $"Account {context.Transaction.AccountId} has {recentTransactions} transactions in the last {_timeWindow.TotalHours} hour(s), exceeding limit of {_maxTransactionsPerHour}");
        }

        return FraudRuleEvaluationResult.RuleNotTriggered(RuleName);
    }
}