using FraudRuleEngine.Transactions.Api.Services.Commands;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FraudRuleEngine.Transactions.Api.Tests.Abstractions;
using FraudRuleEngine.Transactions.Api.Domain.Entities;
using FraudRuleEngine.Transactions.Api.Data;
using System.Reflection;
using Newtonsoft.Json;

namespace FraudRuleEngine.Transactions.Api.Tests.Integration;

/// <summary>
/// Tests for transaction rollback scenarios - verifying atomicity when failures occur.
/// </summary>
public class TransactionRollbackTests : BaseIntegrationTest
{
    public TransactionRollbackTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        CleanupDatabase();
    }

    [Fact]
    public async Task CreateTransaction_WhenCurrencyExceedsMaxLength_ShouldReturnValidationError()
    {
        // Arrange - Currency has max length of 3 characters (validation rule)
        // A 4-character currency should be caught by FluentValidation before database operations
        var initialTransactionCount = await DbContext.Transactions.CountAsync();
        var initialOutboxCount = await DbContext.OutboxMessages.CountAsync();

        var command = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 1000m,
            MerchantId: Guid.NewGuid(),
            Currency: "ZARR", // Invalid: exceeds max length of 3
            Timestamp: DateTime.UtcNow,
            ExternalId: $"txn-ext-invalid-currency-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string>()
        );

        // Act
        var result = await Sender.Send(command);

        // Assert - Should fail due to validation error before database operations
        result.IsFailure.Should().BeTrue("Command should fail validation for invalid currency length");
        result.Error.Should().Contain("Currency", "Error message should mention the Currency field");
        result.Error.Should().Contain("3 uppercase letters", "Error message should specify the format requirement");

        // Verify nothing was persisted (validation happens before database operations)
        var finalTransactionCount = await DbContext.Transactions.CountAsync();
        var finalOutboxCount = await DbContext.OutboxMessages.CountAsync();

        var transaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.ExternalId == command.ExternalId);

        var outboxMessage = await DbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.Payload.Contains(command.ExternalId));

        // Verify atomicity: both transaction and outbox should not be created
        finalTransactionCount.Should().Be(initialTransactionCount,
            "Transaction should not be persisted when validation fails");
        finalOutboxCount.Should().Be(initialOutboxCount,
            "Outbox message should not be persisted when validation fails");

        transaction.Should().BeNull("Transaction should not exist after validation failure");
        outboxMessage.Should().BeNull("Outbox message should not exist after validation failure");
    }

    [Fact]
    public async Task CreateTransaction_WhenDuplicateExternalId_ShouldReturnExistingTransactionIdempotently()
    {
        // Arrange - Create a transaction first
        var externalId = $"txn-ext-idempotent-{Guid.NewGuid()}";
        var initialTransactionCount = await DbContext.Transactions.CountAsync();
        var initialOutboxCount = await DbContext.OutboxMessages.CountAsync();

        // Create the first transaction
        var firstCommand = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 1000m,
            MerchantId: Guid.NewGuid(),
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId,
            Metadata: new Dictionary<string, string>()
        );
        var firstResult = await Sender.Send(firstCommand);
        firstResult.IsSuccess.Should().BeTrue("First transaction should succeed");
        var firstTransactionId = firstResult.Value;

        // Act - Send the same ExternalId again (simulating retry/duplicate request)
        var duplicateCommand = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 2000m,              
            MerchantId: Guid.NewGuid(), 
            Currency: "USD",            
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId,     // Same ExternalId - triggers idempotency check
            Metadata: new Dictionary<string, string>()
        );
        var duplicateResult = await Sender.Send(duplicateCommand);

        // Assert 
        duplicateResult.IsSuccess.Should().BeTrue("Duplicate ExternalId should return existing transaction idempotently");
        duplicateResult.Value.Should().Be(firstTransactionId, "Should return the ID of the original transaction");

        var finalTransactionCount = await DbContext.Transactions.CountAsync();
        var finalOutboxCount = await DbContext.OutboxMessages.CountAsync();

        // Should only have ONE transaction and ONE outbox message (no duplicates created)
        finalTransactionCount.Should().Be(initialTransactionCount + 1,
            "Only one transaction should exist (idempotent behavior)");
        finalOutboxCount.Should().Be(initialOutboxCount + 1,
            "Only one outbox message should exist (idempotent behavior)");

        // Verify no duplicate transaction with the new amount was created
        var transactions = await DbContext.Transactions
            .Where(t => t.ExternalId == externalId)
            .ToListAsync();
        
        transactions.Should().HaveCount(1, "Only one transaction should exist with this ExternalId");
        transactions[0].Amount.Should().Be(1000m, "Should have the amount from the first transaction");
        transactions[0].Currency.Should().Be("ZAR", "Should have the currency from the first transaction");
    }
}

