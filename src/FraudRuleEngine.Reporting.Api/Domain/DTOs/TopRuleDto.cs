namespace FraudRuleEngine.Reporting.Api.Domain.DTOs;

public class TopRuleDto
{
    public string RuleName { get; set; } = string.Empty;
    public int TriggerCount { get; set; }
    public decimal AverageRiskScore { get; set; }
    public DateTime Date { get; set; }
}

