using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.ValueObjects;

namespace FraudRuleEngine.Core.Domain;

/// <summary>
/// Interface used for the flagging of transactions.
/// We can add new rules to the system by implementing this interface.
/// Each rule can declare its data requirements and evaluate transactions using a type-safe data context.
/// </summary>
public interface IFraudRule
{
    /// <summary>
    /// The name of the rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Returns the data requirements for this rule. Each requirement is a request object
    /// that will be resolved by the service layer.
    /// </summary>
    /// <param name="transaction">The transaction being evaluated</param>
    /// <returns>Collection of data requests required by this rule (e.g retrieve the number of recent transactions for an account)</returns>
    IEnumerable<IRequest<object>> GetDataRequirements(Shared.Contracts.TransactionReceived transaction);

    /// <summary>
    /// Evaluates the transaction against this rule using the provided context and data.
    /// </summary>
    /// <param name="context">The rule evaluation context containing the transaction</param>
    /// <param name="dataContext">Type-safe data context for accessing required data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The evaluation result</returns>
    Task<FraudRuleEvaluationResult> EvaluateAsync(
        FraudRuleContext context,
        IRuleDataContext dataContext,
        CancellationToken cancellationToken = default);
}
