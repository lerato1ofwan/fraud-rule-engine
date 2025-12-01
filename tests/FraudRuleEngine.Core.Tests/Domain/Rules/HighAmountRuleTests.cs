using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.Rules;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace FraudRuleEngine.Core.Tests.Domain.Rules;

public class HighAmountRuleTests
{
    [Theory]
    [InlineData(15000, true, 0.7, "Transaction amount 15000 exceeds threshold 10000")]
    [InlineData(10000, false, 0, null)]
    [InlineData(9999.99, false, 0, null)]
    public async Task EvaluateAsync_AmountExceedsThreshold_ShouldTriggerRule(
        decimal amount, bool expectedTriggered, decimal expectedRiskScore, string? expectedReasonContains)
    {
        // Arrange
        var rule = new HighAmountRule(threshold: 10000);
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            Amount = amount,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", "RSA" } }
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>(); // Not used by HighAmountRule

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().Be(expectedTriggered);
        result.RiskScore.Should().Be(expectedRiskScore);
        result.RuleName.Should().Be("HighAmountRule");
        if (expectedReasonContains != null)
        {
            result.Reason.Should().Contain(expectedReasonContains);
        }
    }

    [Fact]
    public async Task EvaluateAsync_LargeAmount_ShouldReturnHighRiskScore()
    {
        // Arrange
        var rule = new HighAmountRule(threshold: 10000);
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 50000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeTrue();
        result.RiskScore.Should().Be(0.7m);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnCorrectRuleName()
    {
        // Arrange
        var rule = new HighAmountRule(threshold: 10000);
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 5000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.RuleName.Should().Be("HighAmountRule");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldIncludeAmountInReason()
    {
        // Arrange
        var rule = new HighAmountRule(threshold: 10000);
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
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Reason.Should().Contain("15000");
        result.Reason.Should().Contain("10000");
    }

    [Fact]
    public void GetDataRequirements_ShouldReturnEmptyCollection()
    {
        // Arrange
        var rule = new HighAmountRule(threshold: 10000);
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 5000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var requirements = rule.GetDataRequirements(transaction);

        // Assert
        requirements.Should().BeEmpty();
    }
}

