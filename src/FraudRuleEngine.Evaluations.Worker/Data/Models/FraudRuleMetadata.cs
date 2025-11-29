namespace FraudRuleEngine.Evaluations.Worker.Data.Models;

public class FraudRuleMetadata
{
    public int Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

