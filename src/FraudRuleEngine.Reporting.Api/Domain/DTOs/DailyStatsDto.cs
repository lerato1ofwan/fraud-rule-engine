namespace FraudRuleEngine.Reporting.Api.Domain.DTOs;

public class DailyStatsDto
{
    public DateTime Date { get; set; }
    public int TotalEvaluations { get; set; }
    public int FlaggedCount { get; set; }
    public decimal AverageRiskScore { get; set; }
}

