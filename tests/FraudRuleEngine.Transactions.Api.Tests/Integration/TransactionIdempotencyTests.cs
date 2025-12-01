using FraudRuleEngine.Shared.Common;
using FraudRuleEngine.Transactions.Api.Data;
using FraudRuleEngine.Transactions.Api.Services.Commands;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FraudRuleEngine.Transactions.Api.Tests.Abstractions;

namespace FraudRuleEngine.Transactions.Api.Tests.Integration;

/// <summary>
/// Tests for transaction idempotency via ExternalId.
/// </summary>
public class TransactionIdempotencyTests : BaseIntegrationTest
{
    public TransactionIdempotencyTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        CleanupDatabase();
    }

    [Fact]
    public async Task CreateTransaction_DuplicateExternalId_ShouldReturnExistingTransactionId()
    {
        // Arrange
        var externalId = $"txn-ext-duplicate-{Guid.NewGuid()}";
        var accountId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        
        var firstCommand = new CreateTransactionCommand(
            AccountId: accountId,
            Amount: 1000m,
            MerchantId: merchantId,
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId,
            Metadata: new Dictionary<string, string>()
        );

        var firstResult = await Sender.Send(firstCommand);
        firstResult.IsSuccess.Should().BeTrue();
        var firstTransactionId = firstResult.Value;

        // Act - Try to create the same transaction again
        var duplicateCommand = new CreateTransactionCommand(
            AccountId: accountId,
            Amount: 2000m, // Different amount
            MerchantId: merchantId,
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId, // Same external ID
            Metadata: new Dictionary<string, string>()
        );

        var duplicateResult = await Sender.Send(duplicateCommand);

        // Assert
        duplicateResult.IsSuccess.Should().BeTrue();
        duplicateResult.Value.Should().Be(firstTransactionId, "Should return existing transaction ID for duplicate external ID");
    }

    [Fact]
    public async Task CreateTransaction_DuplicateExternalId_ShouldNotCreateDuplicateRecord()
    {
        // Arrange
        var externalId = $"txn-ext-no-duplicate-{Guid.NewGuid()}";
        var accountId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();

        var firstCommand = new CreateTransactionCommand(
            AccountId: accountId,
            Amount: 1000m,
            MerchantId: merchantId,
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId,
            Metadata: new Dictionary<string, string>()
        );

        await Sender.Send(firstCommand);

        // Act - Try to create the same transaction again
        var duplicateCommand = new CreateTransactionCommand(
            AccountId: accountId,
            Amount: 2000m,
            MerchantId: merchantId,
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId,
            Metadata: new Dictionary<string, string>()
        );

        await Sender.Send(duplicateCommand);

        // Assert - Verify only one record exists
        var transactions = await DbContext.Transactions
            .Where(t => t.ExternalId == externalId)
            .ToListAsync();

        transactions.Should().HaveCount(1, "Should not create duplicate record for same external ID");
    }
}

