# Fraud Rules Documentation

## Overview

The fraud rule engine uses a flexible design that makes it easy to add new rules without touching existing code. Each rule is independent and testable, and they all work together through a pipeline that aggregates their results.

## Architecture

The rule engine follows clean architecture principles:

- **Domain Layer** (`FraudRuleEngine.Core`): Contains all the rule logic and interfaces. No infrastructure dependencies here - it's pure business logic.
- **Infrastructure Layer** (`FraudRuleEngine.Evaluations.Worker`): Handles data access, database queries, and ties everything together.

This separation means we can test rules in isolation without needing a database or external services.

## Design Patterns

### Strategy Pattern

Each fraud rule implements `IFraudRule`. This lets us swap rules in and out without changing the pipeline code.

```csharp
public interface IFraudRule
{
    string RuleName { get; }
    Task<FraudRuleEvaluationResult> EvaluateAsync(
        FraudRuleContext context, 
        CancellationToken cancellationToken = default);
}
```

**Why this works well**:
- Rules are independent - test them separately
- Easy to add new rules - just implement the interface
- Can enable/disable rules dynamically
- Each rule can have different dependencies

### Composite Pattern

The `CompositeRulePipeline` runs all the rules and combines their results. It's a single entry point that handles the orchestration.

**Benefits**:
- Consistent result aggregation
- Easy to add/remove rules
- Can optimize the pipeline (parallel evaluation, early exit, etc.)

### Mediator Pattern (for Data Fetching)

Rules sometimes need data from the database (like transaction history). Instead of rules directly calling repositories, we use a mediator pattern:

```
Rule needs data → Creates a Request object → Mediator finds the Handler → Handler fetches data → Rule gets typed data
```

This eliminates magic strings and tight coupling. The rule says "I need recent transaction count" and the mediator figures out how to get it.

**Key components**:
- `IRequest<TResponse>`: Marker interface for data requests
- `IRequestHandler<TRequest, TResponse>`: Handler that fetches the data
- `IRuleDataContext`: The mediator that routes requests to handlers

### Specification Pattern

We use specifications to filter and validate rule results. For example, `HighRiskSpecification` filters results where the risk score is above a threshold.

**Benefits**:
- Reusable business logic
- Composable - can combine specifications
- Clear intent in the code

### Repository Pattern

Data access is abstracted through repositories. This makes rules testable (we can mock repositories) and keeps business logic separate from database concerns.

## Implemented Rules

### HighAmountRule

**What it does**: Flags transactions that exceed a threshold amount.

**Configuration**: 
- `Threshold` (default: 10,000) - the maximum amount before triggering

**How it works**:
```csharp
if (transaction.Amount > threshold)
    return Triggered(riskScore: 0.7, reason: "Amount exceeds threshold")
else
    return NotTriggered()
```

**Risk Score**: 0.7 when triggered

**Example**: A R15,000 transaction with a R10,000 threshold triggers with risk score 0.7.

### VelocityRule

**What it does**: Detects when an account is making too many transactions in a short time window.

**Configuration**:
- `MaxTransactionsPerHour` (default: 10) - how many transactions are allowed
- `TimeWindow` (default: 1 hour) - the time window to check

**How it works**:
```csharp
recentCount = GetRecentTransactionsCount(accountId, timeWindow)
if (recentCount >= maxTransactionsPerHour)
    return Triggered(riskScore: 0.8, reason: "High transaction velocity")
else
    return NotTriggered()
```

**Risk Score**: 0.8 when triggered

**Dependencies**: Needs `ITransactionHistoryRepository` to query past transactions. This is handled through the mediator pattern - the rule creates a `RecentTransactionCountRequest` and the mediator finds the handler that can fulfill it.

**Example**: An account with 12 transactions in the last hour when the limit is 10 triggers with risk score 0.8.

### ForeignCountryRule

**What it does**: Flags transactions from countries other than the allowed one.

**Configuration**:
- `AllowedCountry` (default: "RSA") - the country code that's allowed

**How it works**:
```csharp
country = transaction.Metadata["Country"]
if (country != allowedCountry)
    return Triggered(riskScore: 0.6, reason: "Foreign country transaction")
else
    return NotTriggered()
```

**Risk Score**: 0.6 when triggered

**Metadata Required**: The transaction metadata must include a "Country" field.

**Example**: A transaction from "UK" when "RSA" is allowed triggers with risk score 0.6.

## How Rule Evaluation Works

### 1. Context Creation

When a transaction comes in, we create a `FraudRuleContext` with the transaction data and any additional context needed.

### 2. Rule Execution

Each rule is evaluated independently. Rules that need data from the database use the mediator pattern to get it.

### 3. Result Aggregation

After all rules run, we aggregate the results:
- `OverallRiskScore`: Average of all triggered rule risk scores
- `IsFlagged`: True if `OverallRiskScore >= 0.5`
- `RuleResults`: All rule results (both triggered and not triggered)

### 4. Fraud Determination

A transaction is flagged as fraud if the overall risk score is 0.5 or higher. The risk score is the average of all triggered rules.

**Example**:
- HighAmountRule triggers: 0.7
- VelocityRule doesn't trigger: 0.0
- ForeignCountryRule triggers: 0.6
- OverallRiskScore: (0.7 + 0.6) / 2 = 0.65
- IsFlagged: true (because 0.65 >= 0.5)

## Adding a New Rule

Here's how to add a new rule without touching existing code:

### Step 1: Create the Data Request (if needed)

If your rule needs data from the database, create a request object:

```csharp
public class AverageTransactionAmountRequest : IRequest<decimal>
{
    public string RequestId => "AverageTransactionAmount";
    public Guid AccountId { get; init; }
    public TimeSpan TimeWindow { get; init; }
}
```

### Step 2: Create the Handler (if you created a request)

Create a handler that fetches the data:

```csharp
public class AverageTransactionAmountHandler 
    : IRequestHandler<AverageTransactionAmountRequest, decimal>
{
    private readonly ITransactionHistoryRepository _repository;

    public async Task<decimal> HandleAsync(
        AverageTransactionAmountRequest request, 
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetAverageAmountAsync(
            request.AccountId, 
            request.TimeWindow, 
            cancellationToken);
    }
}
```

### Step 3: Implement the Rule

Create your rule class:

```csharp
public class AverageAmountRule : IFraudRule
{
    public string RuleName => "AverageAmountRule";
    private readonly decimal _threshold;

    public AverageAmountRule(decimal threshold = 5000)
    {
        _threshold = threshold;
    }

    public async Task<FraudRuleEvaluationResult> EvaluateAsync(
        FraudRuleContext context,
        IRuleDataContext dataContext,
        CancellationToken cancellationToken = default)
    {
        var request = AverageTransactionAmountRequest.FromTransaction(
            context.Transaction, 
            TimeSpan.FromDays(30));
        
        var averageAmount = await dataContext.ResolveAsync<AverageTransactionAmountRequest, decimal>(
            request, cancellationToken);

        if (context.Transaction.Amount > averageAmount * 3)
        {
            return FraudRuleEvaluationResult.RuleTriggered(
                RuleName,
                0.75m,
                $"Transaction amount {context.Transaction.Amount} is 3x the 30-day average of {averageAmount}");
        }

        return FraudRuleEvaluationResult.RuleNotTriggered(RuleName);
    }
}
```

### Step 4: Register Everything

In `Program.cs`, register the handler and rule:

```csharp
// Register the handler
builder.Services.AddScoped<
    IRequestHandler<AverageTransactionAmountRequest, decimal>, 
    AverageTransactionAmountHandler>();

// Register the rule
builder.Services.AddScoped<IFraudRule, AverageAmountRule>(sp =>
    new AverageAmountRule(fraudRulesConfig.GetValue<decimal>("AverageAmountRule:Threshold")));
```

That's it! The `CompositeRulePipeline` automatically picks up all registered `IFraudRule` implementations, so your new rule will run automatically.

## Rule Configuration

Rules can be configured in a few ways:

1. **Constructor parameters**: Simple configuration passed when creating the rule
2. **appsettings.json**: Configuration values loaded from config
3. **Database**: The `fraud_rules_metadata` table allows dynamic configuration (enable/disable, adjust thresholds)

For dynamic configuration, you can update the database:

```sql
UPDATE fraud_rules_metadata 
SET configuration = '{"threshold": 15000}' 
WHERE rule_name = 'HighAmountRule';
```

## Testing Rules

### Unit Tests

We test each rule in isolation:

```csharp
[Fact]
public async Task HighAmountRule_ShouldTrigger_WhenAmountExceedsThreshold()
{
    // Arrange
    var rule = new HighAmountRule(threshold: 1000);
    var context = new FraudRuleContext
    {
        Transaction = new TransactionReceived { Amount = 2000 }
    };
    
    // Act
    var result = await rule.EvaluateAsync(context);
    
    // Assert
    Assert.True(result.Triggered);
    Assert.Equal(0.7m, result.RiskScore);
}
```

### Integration Tests

We test the full pipeline with multiple rules to make sure aggregation works correctly.

## Performance Considerations

**Optimization strategies**:
- Rules can be evaluated in parallel (future enhancement)
- Cache expensive lookups like transaction history
- Early exit if fraud is already determined (future enhancement)
- Prioritize high-impact rules first (future enhancement)

**What we monitor**:
- Average evaluation time per rule
- Total pipeline evaluation time
- How often each rule triggers
- False positive/negative rates

## Rule Maintenance

**Monitoring**: We track how often rules trigger and their effectiveness. This helps us tune thresholds and identify rules that aren't working well.

**Tuning**: Based on historical data, we adjust risk scores and thresholds. Rules can be enabled/disabled dynamically through the database.

**Versioning**: In the future, we might version rules for A/B testing or gradual rollouts.

## Best Practices

1. **Single Responsibility**: Each rule should check one thing
2. **Idempotency**: Same input should always produce the same output
3. **Performance**: Keep rule evaluation fast - aim for under 100ms
4. **Logging**: Log rule decisions so we can audit what happened
5. **Testing**: Write comprehensive tests for each rule
6. **Documentation**: Document what the rule does and why

## Future Enhancements

Some ideas for the future:
- Machine learning-based rules
- Rules that adapt based on patterns
- A/B testing different rule variations
- Real-time rule updates without redeployment
- Rule marketplace for sharing rules across teams
