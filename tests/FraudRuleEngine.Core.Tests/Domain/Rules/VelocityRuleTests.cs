using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.Rules;
using FraudRuleEngine.Core.Domain.ValueObjects;
using FraudRuleEngine.Shared.Contracts;
using FluentAssertions;
using Moq;
using Xunit;

namespace FraudRuleEngine.Core.Tests.Domain.Rules;

public class VelocityRuleTests
{
    [Theory]
    [InlineData(11, true, 0.8)]
    [InlineData(10, true, 0.8)]
    [InlineData(9, false, 0)]
    public async Task EvaluateAsync_TransactionCountExceedsLimit_ShouldTriggerRule(
        int recentTransactionCount, bool expectedTriggered, decimal expectedRiskScore)
    {
        // Arrange
        var rule = new VelocityRule(maxTransactionsPerHour: 10);
        var accountId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");
        var transactionTimestamp = DateTime.UtcNow;
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = transactionTimestamp,
            Metadata = new Dictionary<string, string>()
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();
        
        var expectedRequest = RecentTransactionCountRequest.FromTransaction(transaction, TimeSpan.FromHours(1));
        mockDataContext
            .Setup(x => x.ResolveAsync<RecentTransactionCountRequest, int>(
                It.Is<RecentTransactionCountRequest>(r => 
                    r.AccountId == accountId && 
                    r.Since <= transactionTimestamp &&
                    r.Since >= transactionTimestamp.AddHours(-1).AddSeconds(-1)), // Allow small time variance
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentTransactionCount);

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().Be(expectedTriggered);
        result.RiskScore.Should().Be(expectedRiskScore);
        result.RuleName.Should().Be("VelocityRule");
        
        // Verify that ResolveAsync was called with the correct request
        mockDataContext.Verify(
            x => x.ResolveAsync<RecentTransactionCountRequest, int>(
                It.Is<RecentTransactionCountRequest>(r => 
                    r.AccountId == accountId && 
                    r.Since <= transactionTimestamp &&
                    r.Since >= transactionTimestamp.AddHours(-1).AddSeconds(-1)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void GetDataRequirements_ShouldReturnRecentTransactionCountRequest()
    {
        // Arrange
        var rule = new VelocityRule(maxTransactionsPerHour: 10);
        var accountId = Guid.NewGuid();
        var transactionTimestamp = DateTime.UtcNow;
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = transactionTimestamp,
            Metadata = new Dictionary<string, string>()
        };

        // Act
        var requirements = rule.GetDataRequirements(transaction).ToList();

        // Assert
        requirements.Should().HaveCount(1, "VelocityRule should declare exactly one data requirement");
        
        // Verify the RequestId matches what RecentTransactionCountRequest should have
        var requirement = requirements.First();
        requirement.RequestId.Should().Be("RecentTransactionCount", 
            "The requirement should have the RequestId matching RecentTransactionCountRequest");
        
        // Note: We cannot directly cast IRequest<object> back to RecentTransactionCountRequest
        // because of the type system. The actual request creation and property validation
        // is tested indirectly through EvaluateAsync_ShouldCallResolveAsyncWithCorrectRequest
    }

    [Fact]
    public async Task EvaluateAsync_ShouldCallResolveAsyncWithCorrectRequest()
    {
        // Arrange
        var rule = new VelocityRule(maxTransactionsPerHour: 10);
        var accountId = Guid.NewGuid();
        var transactionTimestamp = DateTime.UtcNow;
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = transactionTimestamp,
            Metadata = new Dictionary<string, string>()
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();
        
        var expectedRequest = RecentTransactionCountRequest.FromTransaction(transaction, TimeSpan.FromHours(1));
        mockDataContext
            .Setup(x => x.ResolveAsync<RecentTransactionCountRequest, int>(
                It.IsAny<RecentTransactionCountRequest>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert - Verify that ResolveAsync was called with the correct request parameters
        mockDataContext.Verify(
            x => x.ResolveAsync<RecentTransactionCountRequest, int>(
                It.Is<RecentTransactionCountRequest>(r => 
                    r.AccountId == accountId && 
                    r.Since <= transactionTimestamp &&
                    r.Since >= transactionTimestamp.AddHours(-1).AddSeconds(-1)),
                It.IsAny<CancellationToken>()),
            Times.Once, 
            "EvaluateAsync should call ResolveAsync exactly once with a request matching the transaction's account ID and time window");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldHandleTimeWindowVariations()
    {
        // Arrange
        var customTimeWindow = TimeSpan.FromHours(2);
        var rule = new VelocityRule(maxTransactionsPerHour: 10, timeWindow: customTimeWindow);
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
        
        mockDataContext
            .Setup(x => x.ResolveAsync<RecentTransactionCountRequest, int>(It.IsAny<RecentTransactionCountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeTrue();
        result.Reason.Should().Contain("2 hour(s)");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldHandleZeroRecentTransactions()
    {
        // Arrange
        var rule = new VelocityRule(maxTransactionsPerHour: 10);
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
        
        mockDataContext
            .Setup(x => x.ResolveAsync<RecentTransactionCountRequest, int>(It.IsAny<RecentTransactionCountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeFalse();
        result.RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldIncludeAccountIdInReason()
    {
        // Arrange
        var rule = new VelocityRule(maxTransactionsPerHour: 10);
        var accountId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");
        var transaction = new TransactionReceived
        {
            TransactionId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 1000m,
            Currency = "ZAR",
            MerchantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };
        var context = new FraudRuleContext { Transaction = transaction };
        var mockDataContext = new Mock<IRuleDataContext>();
        
        mockDataContext
            .Setup(x => x.ResolveAsync<RecentTransactionCountRequest, int>(It.IsAny<RecentTransactionCountRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var result = await rule.EvaluateAsync(context, mockDataContext.Object);

        // Assert
        result.Triggered.Should().BeTrue();
        result.Reason.Should().Contain(accountId.ToString());
        result.Reason.Should().Contain("15");
        result.Reason.Should().Contain("10");
    }
}

