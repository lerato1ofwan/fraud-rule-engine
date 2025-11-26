using FraudRuleEngine.Core.Domain.ValueObjects;

namespace FraudRuleEngine.Evaluations.Worker.Data.Models;

public class FraudCheck : Entity
{
    public Guid FraudCheckId { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public bool IsFlagged { get; private set; }
    public decimal OverallRiskScore { get; private set; }
    public DateTime EvaluatedAt { get; private set; }
    public List<FraudRuleResult> RuleResults { get; private set; } = new();

    private FraudCheck() { }

    private FraudCheck(
        Guid transactionId,
        Guid accountId,
        bool isFlagged,
        decimal overallRiskScore,
        List<FraudRuleResult> ruleResults)
    {
        FraudCheckId = Guid.NewGuid();
        TransactionId = transactionId;
        AccountId = accountId;
        IsFlagged = isFlagged;
        OverallRiskScore = overallRiskScore;
        EvaluatedAt = DateTime.UtcNow;
        RuleResults = ruleResults;
    }

    public static FraudCheck Create(
        Guid transactionId,
        Guid accountId,
        bool IsFlagged,
        decimal overallRiskScore,
        List<FraudRuleResult> ruleResults)
    {
        return new FraudCheck(transactionId, accountId, IsFlagged, overallRiskScore, ruleResults);
    }
}