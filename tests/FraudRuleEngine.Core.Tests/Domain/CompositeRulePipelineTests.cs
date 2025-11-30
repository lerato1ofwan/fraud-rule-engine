using FraudRuleEngine.Core.Domain;
using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.Rules;
using FraudRuleEngine.Core.Domain.Specifications;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace FraudRuleEngine.Core.Tests.Domain;

public class CompositeRulePipelineTests
{
    [Fact]
    public async Task EvaluateAsync_MultipleRulesTriggered_ShouldAggregateRiskScores()
    {
        // Arrange
        var highAmountRule = new HighAmountRule(threshold: 10000);
        var foreignCountryRule = new ForeignCountryRule(allowedCountry: "RSA");
        var rules = new List<IFraudRule> { highAmountRule, foreignCountryRule };
        var pipeline = new CompositeRulePipeline(rules);

        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 15000m, // Triggers HighAmountRule
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", "USA" } } // Triggers ForeignCountryRule
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await pipeline.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.RuleResults.Should().HaveCount(2);
        result.RuleResults.Count(r => r.Triggered).Should().Be(2);
        
        // Average of 0.7 (HighAmountRule) and 0.6 (ForeignCountryRule) = 0.65
        result.OverallRiskScore.Should().Be(0.65m);
        result.IsFlagged.Should().BeTrue("Risk score 0.65 >= 0.5 should flag transaction");
    }

    [Fact]
    public async Task EvaluateAsync_OverallRiskScoreAboveThreshold_ShouldFlagTransaction()
    {
        // Arrange
        var rule = new HighAmountRule(threshold: 10000);
        var rules = new List<IFraudRule> { rule };
        var pipeline = new CompositeRulePipeline(rules);

        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 15000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await pipeline.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.OverallRiskScore.Should().Be(0.7m);
        result.IsFlagged.Should().BeTrue("Risk score 0.7 >= 0.5 should flag transaction");
    }

    [Fact]
    public async Task EvaluateAsync_OverallRiskScoreBelowThreshold_ShouldNotFlagTransaction()
    {
        // Arrange
        var rule = new ForeignCountryRule(allowedCountry: "RSA");
        var rules = new List<IFraudRule> { rule };
        var pipeline = new CompositeRulePipeline(rules);

        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", "RSA" } }
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await pipeline.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.OverallRiskScore.Should().Be(0m);
        result.IsFlagged.Should().BeFalse("Risk score 0 < 0.5 should not flag transaction");
    }

    [Fact]
    public async Task EvaluateAsync_NoRulesTriggered_ShouldReturnZeroRiskScore()
    {
        // Arrange
        var highAmountRule = new HighAmountRule(threshold: 10000);
        var foreignCountryRule = new ForeignCountryRule(allowedCountry: "RSA");
        var rules = new List<IFraudRule> { highAmountRule, foreignCountryRule };
        var pipeline = new CompositeRulePipeline(rules);

        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 5000m, 
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", "RSA" } } 
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await pipeline.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.OverallRiskScore.Should().Be(0m);
        result.IsFlagged.Should().BeFalse();
        result.RuleResults.Count(r => r.Triggered).Should().Be(0);
    }

    [Fact]
    public void GetAllDataRequirements_ShouldCollectAllDataRequirements()
    {
        // Arrange
        var highAmountRule = new HighAmountRule(threshold: 10000);
        var velocityRule = new VelocityRule(maxTransactionsPerHour: 10);
        var foreignCountryRule = new ForeignCountryRule(allowedCountry: "RSA");
        var rules = new List<IFraudRule> { highAmountRule, velocityRule, foreignCountryRule };
        var pipeline = new CompositeRulePipeline(rules);

        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var requirements = pipeline.GetAllDataRequirements(transaction).ToList();

        // Assert
        // HighAmountRule: 0 requirements
        // VelocityRule: 1 requirement (RecentTransactionCountRequest)
        // ForeignCountryRule: 0 requirements
        requirements.Should().HaveCount(1);
        
        // The requirement is wrapped in RequestWrapper, so we verify by RequestId instead of type
        requirements[0].RequestId.Should().Be("RecentTransactionCount", 
            "The requirement should be a RecentTransactionCountRequest (wrapped)");
    }

    [Fact]
    public async Task EvaluateAsync_EmptyRuleList_ShouldReturnNoResults()
    {
        // Arrange
        var rules = new List<IFraudRule>();
        var pipeline = new CompositeRulePipeline(rules);

        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await pipeline.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.RuleResults.Should().BeEmpty();
        result.OverallRiskScore.Should().Be(0m);
        result.IsFlagged.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_WithSpecification_ShouldFilterResults()
    {
        // Arrange - Create a specification that only accepts high-risk results (risk >= 0.7)
        var highRiskSpec = new HighRiskSpecification(0.7m);
        var highAmountRule = new HighAmountRule(threshold: 10000);
        var foreignCountryRule = new ForeignCountryRule(allowedCountry: "RSA");
        var rules = new List<IFraudRule> { highAmountRule, foreignCountryRule };
        var pipeline = new CompositeRulePipeline(rules, highRiskSpec);

        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 15000m, // Triggers HighAmountRule (0.7 risk)
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", "USA" } } // Triggers ForeignCountryRule (0.6 risk)
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var allResults = await pipeline.EvaluateAllAsync(context, mockDataContext.Object);

        // Assert
        // Only HighAmountRule result (0.7) should pass the specification
        allResults.Should().HaveCount(1);
        allResults[0].RuleName.Should().Be("HighAmountRule");
        allResults[0].RiskScore.Should().Be(0.7m);
    }

    private class HighRiskSpecification : ISpecification<FraudRuleEvaluationResult>
    {
        private readonly decimal _minRiskScore;

        public HighRiskSpecification(decimal minRiskScore)
        {
            _minRiskScore = minRiskScore;
        }

        public bool IsSatisfiedBy(FraudRuleEvaluationResult candidate)
        {
            return candidate.RiskScore >= _minRiskScore;
        }
    }
}

