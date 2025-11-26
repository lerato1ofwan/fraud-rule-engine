using FraudRuleEngine.Evaluations.Worker.Data.Models;
using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Evaluations.Worker.Services;

public interface IFraudEvaluationService
{
    Task<FraudCheck> EvaluateAsync(TransactionReceived transaction, CancellationToken cancellationToken = default);
}
