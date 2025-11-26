namespace FraudRuleEngine.Reporting.Api.Data.Models;

public class FraudRuleHeatmap
{
    public int Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int TriggerCount { get; set; }
    public decimal AverageRiskScore { get; set; }
    public DateTime LastUpdated { get; set; }
}

