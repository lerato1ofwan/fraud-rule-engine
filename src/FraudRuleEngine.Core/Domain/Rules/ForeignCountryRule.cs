using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Core.Domain.Rules;

/// <summary>
/// Rule that checks if a transaction originates from a foreign country.
/// This rule requires no external data - it uses transaction metadata.
/// </summary>
public class ForeignCountryRule : IFraudRule
{
    public string RuleName => "ForeignCountryRule";
    private readonly string _allowedCountry;

    public ForeignCountryRule(string allowedCountry = "RSA")
    {
        _allowedCountry = allowedCountry;
    }

    public IEnumerable<IRequest<object>> GetDataRequirements(TransactionReceived transaction)
    {
        yield break;
    }

    public Task<FraudRuleEvaluationResult> EvaluateAsync(
        FraudRuleContext context,
        IRuleDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        var country = context.Transaction.Metadata.GetValueOrDefault("Country", _allowedCountry);
        
        if (!string.Equals(country, _allowedCountry, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(FraudRuleEvaluationResult.RuleTriggered(
                RuleName,
                0.6m,
                $"Transaction from foreign country: {country}"));
        }

        return Task.FromResult(FraudRuleEvaluationResult.RuleNotTriggered(RuleName));
    }
}
