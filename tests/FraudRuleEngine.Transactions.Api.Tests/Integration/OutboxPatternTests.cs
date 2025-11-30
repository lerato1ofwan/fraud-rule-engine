using FraudRuleEngine.Transactions.Api.Data;
using FraudRuleEngine.Transactions.Api.Domain.Events;
using FraudRuleEngine.Transactions.Api.Services.Commands;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xunit;
using FraudRuleEngine.Transactions.Api.Tests.Abstractions;

namespace FraudRuleEngine.Transactions.Api.Tests.Integration;

/// <summary>
/// Tests for outbox pattern implementation - verifying event persistence and atomicity.
/// </summary>
public class OutboxPatternTests : BaseIntegrationTest
{
    public OutboxPatternTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        CleanupDatabase();
    }

    [Fact]
    public async Task CreateTransaction_ShouldCreateOutboxMessage()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 1000m,
            MerchantId: Guid.NewGuid(),
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: $"txn-ext-outbox-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string> { { "Country", "RSA" } }
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify outbox message was created
        var outboxMessages = await DbContext.OutboxMessages
            .Where(m => m.EventType == "TransactionReceivedEvent")
            .ToListAsync();

        outboxMessages.Should().HaveCount(1, "Should create exactly one outbox message for TransactionReceivedEvent");
        
        var outboxMessage = outboxMessages.First();
        outboxMessage.EventType.Should().Be("TransactionReceivedEvent");
        outboxMessage.ProcessedAt.Should().BeNull("Outbox message should be unprocessed initially");
        outboxMessage.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateTransaction_OutboxMessagePayload_ShouldContainCorrectTransactionData()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        var externalId = $"txn-ext-payload-{Guid.NewGuid()}";
        var timestamp = DateTime.UtcNow.AddMinutes(-10);
        var metadata = new Dictionary<string, string> { { "Country", "RSA" }, { "IPAddress", "192.168.1.1" } };

        var command = new CreateTransactionCommand(
            AccountId: accountId,
            Amount: 5000m,
            MerchantId: merchantId,
            Currency:"ZAR",
            Timestamp: timestamp,
            ExternalId: externalId,
            Metadata: metadata
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var outboxMessage = await DbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.EventType == "TransactionReceivedEvent");

        outboxMessage.Should().NotBeNull();
        outboxMessage!.Payload.Should().NotBeNullOrEmpty();

        // Deserialize the payload to verify it contains correct data
        var deserializedEvent = JsonConvert.DeserializeObject<TransactionReceivedEvent>(outboxMessage.Payload);
        deserializedEvent.Should().NotBeNull();
        deserializedEvent!.TransactionId.Should().Be(result.Value);
        deserializedEvent.AccountId.Should().Be(accountId);
        deserializedEvent.Amount.Should().Be(5000m);
        deserializedEvent.MerchantId.Should().Be(merchantId);
        deserializedEvent.Currency.Should().Be("ZAR");
        deserializedEvent.Timestamp.Should().BeCloseTo(timestamp, TimeSpan.FromSeconds(1));
        deserializedEvent.Metadata.Should().BeEquivalentTo(metadata);
    }

    [Fact]
    public async Task CreateTransaction_OutboxMessage_ShouldBePersistedAtomicallyWithTransaction()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 1000m,
            MerchantId: Guid.NewGuid(),
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: $"txn-ext-atomic-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string>()
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify both transaction and outbox message exist (atomicity)
        var transaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == result.Value);

        var outboxMessage = await DbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.EventType == "TransactionReceivedEvent");

        transaction.Should().NotBeNull("Transaction should be persisted");
        outboxMessage.Should().NotBeNull("Outbox message should be persisted atomically with transaction");

        // Verify the outbox message payload references the correct transaction
        var deserializedEvent = JsonConvert.DeserializeObject<TransactionReceivedEvent>(outboxMessage!.Payload);
        deserializedEvent!.TransactionId.Should().Be(transaction!.TransactionId);
    }

    [Fact]
    public async Task CreateTransaction_MultipleTransactions_ShouldCreateMultipleOutboxMessages()
    {
        // Arrange
        var command1 = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 1000m,
            MerchantId: Guid.NewGuid(),
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: $"txn-ext-multi-1-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string>()
        );

        var command2 = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 2000m,
            MerchantId: Guid.NewGuid(),
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: $"txn-ext-multi-2-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string>()
        );

        // Act
        var result1 = await Sender.Send(command1);
        var result2 = await Sender.Send(command2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var outboxMessages = await DbContext.OutboxMessages
            .Where(m => m.EventType == "TransactionReceivedEvent")
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        outboxMessages.Should().HaveCount(2, "Should create separate outbox message for each transaction");

        // Verify each outbox message has correct transaction data
        var event1 = JsonConvert.DeserializeObject<TransactionReceivedEvent>(outboxMessages[0].Payload);
        var event2 = JsonConvert.DeserializeObject<TransactionReceivedEvent>(outboxMessages[1].Payload);

        event1!.TransactionId.Should().Be(result1.Value);
        event1.Amount.Should().Be(1000m);
        event2!.TransactionId.Should().Be(result2.Value);
        event2.Amount.Should().Be(2000m);
    }

    [Fact]
    public async Task CreateTransaction_OutboxMessage_ShouldHaveCorrectEventStructure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        var command = new CreateTransactionCommand(
            AccountId: accountId,
            Amount: 1000m,
            MerchantId: merchantId,
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: $"txn-ext-structure-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string> { { "Country", "RSA" } }
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var outboxMessage = await DbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.EventType == "TransactionReceivedEvent");

        outboxMessage.Should().NotBeNull();
        outboxMessage!.Id.Should().BeGreaterThan(0);
        outboxMessage.EventType.Should().Be("TransactionReceivedEvent");
        outboxMessage.Payload.Should().NotBeNullOrWhiteSpace();
        outboxMessage.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        outboxMessage.ProcessedAt.Should().BeNull();

        // Verify payload is valid JSON and contains all required fields
        var deserializedEvent = JsonConvert.DeserializeObject<TransactionReceivedEvent>(outboxMessage.Payload);
        deserializedEvent.Should().NotBeNull();
        deserializedEvent!.Id.Should().NotBeEmpty("Event should have a unique ID");
        deserializedEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateTransaction_DuplicateExternalId_ShouldNotCreateDuplicateOutboxMessage()
    {
        // Arrange
        var externalId = $"txn-ext-no-duplicate-outbox-{Guid.NewGuid()}";
        var command1 = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 1000m,
            MerchantId: Guid.NewGuid(),
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId,
            Metadata: new Dictionary<string, string>()
        );

        var command2 = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 2000m,
            MerchantId: Guid.NewGuid(),
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: externalId, // Same external ID
            Metadata: new Dictionary<string, string>()
        );

        // Act
        var result1 = await Sender.Send(command1);
        var result2 = await Sender.Send(command2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result2.Value.Should().Be(result1.Value, "Should return existing transaction ID");

        // Verify only one outbox message was created (for the first transaction)
        var outboxMessages = await DbContext.OutboxMessages
            .Where(m => m.EventType == "TransactionReceivedEvent")
            .ToListAsync();

        // Should have exactly one outbox message since the second command returns existing transaction
        // and doesn't create a new one (no new domain event)
        outboxMessages.Should().HaveCount(1, "Should not create duplicate outbox message for duplicate external ID");

        var deserializedEvent = JsonConvert.DeserializeObject<TransactionReceivedEvent>(outboxMessages[0].Payload);
        deserializedEvent!.TransactionId.Should().Be(result1.Value);
    }

    [Fact]
    public async Task CreateTransaction_OutboxMessagePayload_ShouldSerializeMetadataCorrectly()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "Country", "RSA" },
            { "IPAddress", "192.168.1.100" },
            { "Device", "Mobile" },
            { "Browser", "Chrome" },
            { "UserAgent", "Mozilla/5.0" }
        };

        var command = new CreateTransactionCommand(
            AccountId: Guid.NewGuid(),
            Amount: 1000m,
            MerchantId: Guid.NewGuid(),
            Currency:"ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId: $"txn-ext-metadata-outbox-{Guid.NewGuid()}",
            Metadata: metadata
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var outboxMessage = await DbContext.OutboxMessages
            .FirstOrDefaultAsync(m => m.EventType == "TransactionReceivedEvent");

        outboxMessage.Should().NotBeNull();

        var deserializedEvent = JsonConvert.DeserializeObject<TransactionReceivedEvent>(outboxMessage!.Payload);
        deserializedEvent.Should().NotBeNull();
        deserializedEvent!.Metadata.Should().BeEquivalentTo(metadata, "Metadata should be serialized and deserialized correctly");
        deserializedEvent.Metadata.Should().HaveCount(5);
    }
}

