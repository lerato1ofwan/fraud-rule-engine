using System.Diagnostics.Metrics;

namespace FraudRuleEngine.Shared.Metrics;

public static class FraudMetrics
{
    private static readonly Meter Meter = new("FraudRuleEngine", "1.0.0");

    // Counters
    public static readonly Counter<long> TransactionsReceivedTotal = Meter
        .CreateCounter<long>("fraud_transactions_received", "count", "Total number of transactions received");

    public static readonly Counter<long> FraudChecksTotal = Meter
        .CreateCounter<long>("fraud_checks", "count", "Total number of fraud checks performed");

    public static readonly Counter<long> TransactionsFlaggedTotal = Meter
        .CreateCounter<long>("fraud_transactions_flagged", "count", "Total number of transactions flagged for fraud");

    public static readonly Counter<long> RuleTriggersTotal = Meter
        .CreateCounter<long>("fraud_rule_triggers", "count", "Total number of rule triggers");

    // Histograms
    public static readonly Histogram<double> FraudRiskScore = Meter
        .CreateHistogram<double>("fraud_risk_score", "score", "Distribution of fraud risk scores");

    public static readonly Histogram<double> FraudEvaluationDuration = Meter
        .CreateHistogram<double>("fraud_evaluation_duration_seconds", "seconds", "Time taken to evaluate fraud rules");

    public static readonly UpDownCounter<long> ActiveFraudChecks = Meter
        .CreateUpDownCounter<long>("fraud_active_checks", "count", "Number of active fraud checks in progress");

    public static void IncrementActiveChecks() => ActiveFraudChecks.Add(1);

    public static void DecrementActiveChecks() => ActiveFraudChecks.Add(-1);
}

