using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.Rules;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace FraudRuleEngine.Core.Tests.Domain.Rules;

public class ForeignCountryRuleTests
{
    [Theory]
    [InlineData("USA", true, 0.6)]
    [InlineData("RSA", false, 0)]
    [InlineData("GBR", true, 0.6)]
    public async Task EvaluateAsync_ForeignCountry_ShouldTriggerRule(
        string country, bool expectedTriggered, decimal expectedRiskScore)
    {
        // Arrange
        var rule = new ForeignCountryRule(allowedCountry: "RSA");
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", country } }
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().Be(expectedTriggered);
        result.RiskScore.Should().Be(expectedRiskScore);
        result.RuleName.Should().Be("ForeignCountryRule");
    }

    [Fact]
    public async Task EvaluateAsync_AllowedCountry_ShouldNotTriggerRule()
    {
        // Arrange
        var rule = new ForeignCountryRule(allowedCountry: "RSA");
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
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeFalse();
        result.RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task EvaluateAsync_MissingCountryMetadata_ShouldNotTriggerRule()
    {
        // Arrange - Missing "Country" key should default to allowed country
        var rule = new ForeignCountryRule();
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
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeFalse("Missing Country metadata should default to allowed country (RSA)");
        result.RiskScore.Should().Be(0);
    }

    [Theory]
    [InlineData("rsa")]
    [InlineData("RSA")]
    [InlineData("Rsa")]
    [InlineData("RsA")]
    public async Task EvaluateAsync_CaseInsensitiveCountryComparison_ShouldWork(string country)
    {
        // Arrange
        var rule = new ForeignCountryRule(allowedCountry: "RSA");
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", country } }
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeFalse($"Case-insensitive comparison should treat '{country}' as RSA");
    }

    [Fact]
    public async Task EvaluateAsync_CustomAllowedCountry_ShouldRespectConfiguration()
    {
        // Arrange
        var rule = new ForeignCountryRule(allowedCountry: "USA");
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1000m,
            Currency = "USD",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", "USA" } }
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeFalse("USA should be allowed when configured as allowed country");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldIncludeCountryInReason()
    {
        // Arrange
        var rule = new ForeignCountryRule(allowedCountry: "RSA");
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { { "Country", "USA" } }
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeTrue();
        result.Reason.Should().Contain("USA");
    }
}

