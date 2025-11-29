using System.Diagnostics.Metrics;

namespace FraudRuleEngine.Reporting.Api.Metrics;

public static class ReportingMetrics
{
    private static readonly Meter Meter = new("FraudRuleEngine.Reporting", "1.0.0");

    // Daily Statistics - stored values that are updated by the background service
    private static long _dailyTotalEvaluations = 0;
    private static long _dailyFlaggedCount = 0;
    private static double _dailyAverageRiskScore = 0;
    private static DateTime _lastResetDate = DateTime.UtcNow.Date;
    private static readonly object _dateResetLock = new();

    public static readonly ObservableGauge<long> DailyTotalEvaluations = Meter
        .CreateObservableGauge<long>(
            "reporting_daily_total_evaluations",
            () => Interlocked.Read(ref _dailyTotalEvaluations),
            "count",
            "Total fraud evaluations performed today");

    public static readonly ObservableGauge<long> DailyFlaggedCount = Meter
        .CreateObservableGauge<long>(
            "reporting_daily_flagged_count",
            () => Interlocked.Read(ref _dailyFlaggedCount),
            "count",
            "Total flagged transactions today");

    public static readonly ObservableGauge<double> DailyAverageRiskScore = Meter
        .CreateObservableGauge<double>(
            "reporting_daily_average_risk_score",
            () => Interlocked.CompareExchange(ref _dailyAverageRiskScore, 0, 0),
            "score",
            "Average risk score for today");

    public static void UpdateDailyStats(long totalEvaluations, long flaggedCount, double averageRiskScore)
    {
        var today = DateTime.UtcNow.Date;
        lock (_dateResetLock)
        {
            if (today != _lastResetDate)
            {
                // Reset metrics for new day
                Interlocked.Exchange(ref _dailyTotalEvaluations, 0);
                Interlocked.Exchange(ref _dailyFlaggedCount, 0);
                Interlocked.Exchange(ref _dailyAverageRiskScore, 0);
                _lastResetDate = today;
            }
        }
        
        Interlocked.Exchange(ref _dailyTotalEvaluations, totalEvaluations);
        Interlocked.Exchange(ref _dailyFlaggedCount, flaggedCount);
        Interlocked.Exchange(ref _dailyAverageRiskScore, averageRiskScore);
    }
    
    public static void IncrementDailyStats(bool isFlagged, double riskScore)
    {
        var today = DateTime.UtcNow.Date;
        lock (_dateResetLock)
        {
            if (today != _lastResetDate)
            {
                // Reset metrics for new day
                Interlocked.Exchange(ref _dailyTotalEvaluations, 0);
                Interlocked.Exchange(ref _dailyFlaggedCount, 0);
                Interlocked.Exchange(ref _dailyAverageRiskScore, 0);
                _lastResetDate = today;
            }
        }
        
        var newTotal = Interlocked.Increment(ref _dailyTotalEvaluations);
        if (isFlagged)
        {
            Interlocked.Increment(ref _dailyFlaggedCount);
        }
        
        double currentAvg, newAvg;
        do
        {
            currentAvg = Interlocked.CompareExchange(ref _dailyAverageRiskScore, 0, 0);
            newAvg = newTotal == 1 ? riskScore : ((currentAvg * (newTotal - 1)) + riskScore) / newTotal;
        } while (Interlocked.CompareExchange(ref _dailyAverageRiskScore, newAvg, currentAvg) != currentAvg);
    }

    public static long GetDailyTotalEvaluations() => Interlocked.Read(ref _dailyTotalEvaluations);
    public static long GetDailyFlaggedCount() => Interlocked.Read(ref _dailyFlaggedCount);
    public static double GetDailyAverageRiskScore() => Interlocked.CompareExchange(ref _dailyAverageRiskScore, 0, 0);
}

