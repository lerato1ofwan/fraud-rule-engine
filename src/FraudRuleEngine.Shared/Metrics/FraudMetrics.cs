using System.Diagnostics.Metrics;

namespace FraudRuleEngine.Shared.Metrics;

public static class FraudMetrics
{
    private static readonly Meter Meter = new("FraudRuleEngine", "1.0.0");
    private static long _activeFraudChecks = 0;

    // Counters
    public static readonly Counter<long> TransactionsReceivedTotal = Meter
        .CreateCounter<long>("fraud_transactions_received_total", "count", "Total number of transactions received");

    public static readonly Counter<long> FraudChecksTotal = Meter
        .CreateCounter<long>("fraud_checks_total", "count", "Total number of fraud checks performed");

    public static readonly Counter<long> TransactionsFlaggedTotal = Meter
        .CreateCounter<long>("fraud_transactions_flagged_total", "count", "Total number of transactions flagged for fraud");

    public static readonly Counter<long> RuleTriggersTotal = Meter
        .CreateCounter<long>("fraud_rule_triggers_total", "count", "Total number of rule triggers");

    // Histograms
    public static readonly Histogram<double> FraudRiskScore = Meter
        .CreateHistogram<double>("fraud_risk_score", "score", "Distribution of fraud risk scores");

    public static readonly Histogram<double> FraudEvaluationDuration = Meter
        .CreateHistogram<double>("fraud_evaluation_duration_seconds", "seconds", "Time taken to evaluate fraud rules");

    // Gauges
    public static readonly ObservableGauge<long> ActiveFraudChecks = Meter
        .CreateObservableGauge<long>(
            "fraud_active_checks", 
            () => Interlocked.Read(ref _activeFraudChecks),
            "Number of active fraud checks in progress");

    public static void IncrementActiveChecks() => Interlocked.Increment(ref _activeFraudChecks);
    public static void DecrementActiveChecks() => Interlocked.Decrement(ref _activeFraudChecks);
}

