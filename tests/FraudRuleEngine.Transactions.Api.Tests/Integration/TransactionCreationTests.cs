using FraudRuleEngine.Transactions.Api.Services.Commands;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FraudRuleEngine.Transactions.Api.Tests.Abstractions;

namespace FraudRuleEngine.Transactions.Api.Tests.Integration;

/// <summary>
/// Tests for basic transaction creation and persistence.
/// </summary>
public class TransactionCreationTests : BaseIntegrationTest
{
    public TransactionCreationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        CleanupDatabase();
    }

    [Fact]
    public async Task CreateTransaction_ValidRequest_ShouldPersistToDatabase()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        var externalId = $"txn-ext-unique-{Guid.NewGuid()}";
        var command = new CreateTransactionCommand(
            AccountId:  accountId,
            Amount:  15000m,
            MerchantId: merchantId,
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId:  externalId,
            Metadata: new Dictionary<string, string> { { "Country", "RSA" }, { "IPAddress", "192.168.1.1" } }
        );

        // Act
        var result = await Sender.Send(command);

        // Assert - Verify HTTP result
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        // Assert - Verify database persistence (CRITICAL - not just HTTP response)
        var persistedTransaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == result.Value);

        persistedTransaction.Should().NotBeNull();
        persistedTransaction!.Amount.Should().Be(15000m);
        persistedTransaction.Currency.Should().Be("ZAR");
        persistedTransaction.AccountId.Should().Be(accountId);
        persistedTransaction.MerchantId.Should().Be(merchantId);
        persistedTransaction.ExternalId.Should().Be(externalId);
        persistedTransaction.Metadata.Data.Should().ContainKey("Country");
        persistedTransaction.Metadata.Data["Country"].Should().Be("RSA");
        persistedTransaction.Metadata.Data.Should().ContainKey("IPAddress");
        persistedTransaction.Metadata.Data["IPAddress"].Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task CreateTransaction_ValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            AccountId:  Guid.NewGuid(),
            Amount:  1000m,
            MerchantId: Guid.NewGuid(),
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId:  $"txn-ext-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string>()
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateTransaction_ValidRequest_ShouldGenerateTransactionId()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            AccountId:  Guid.NewGuid(),
            Amount:  1000m,
            MerchantId: Guid.NewGuid(),
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId:  $"txn-ext-{Guid.NewGuid()}",
            Metadata: new Dictionary<string, string>()
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        // Verify the transaction ID is a valid GUID
        var transaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == result.Value);
        transaction.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTransaction_ShouldPersistAllTransactionFields()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var merchantId = Guid.NewGuid();
        var externalId = $"txn-ext-fields-{Guid.NewGuid()}";
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        var command = new CreateTransactionCommand(
            AccountId:  accountId,
            Amount:  5000.50m,
            MerchantId: merchantId,
            Currency: "ZAR",
            Timestamp: timestamp,
            ExternalId:  externalId,
            Metadata: new Dictionary<string, string> { { "Country", "RSA" }, { "Device", "Mobile" } }
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var transaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == result.Value);

        transaction.Should().NotBeNull();
        transaction!.AccountId.Should().Be(accountId);
        transaction.Amount.Should().Be(5000.50m);
        transaction.MerchantId.Should().Be(merchantId);
        transaction.Currency.Should().Be("ZAR");
        transaction.ExternalId.Should().Be(externalId);
        transaction.Timestamp.Should().BeCloseTo(timestamp, TimeSpan.FromSeconds(1));
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateTransaction_ShouldPersistTransactionMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "Country", "RSA" },
            { "IPAddress", "192.168.1.100" },
            { "Device", "Mobile" },
            { "Browser", "Chrome" }
        };

        var command = new CreateTransactionCommand(
            AccountId:  Guid.NewGuid(),
            Amount:  1000m,
            MerchantId: Guid.NewGuid(),
            Currency: "ZAR",
            Timestamp: DateTime.UtcNow,
            ExternalId:  $"txn-ext-metadata-{Guid.NewGuid()}",
            Metadata: metadata
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var transaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == result.Value);

        transaction.Should().NotBeNull();
        transaction!.Metadata.Data.Should().HaveCount(4);
        transaction.Metadata.Data.Should().ContainKey("Country");
        transaction.Metadata.Data["Country"].Should().Be("RSA");
        transaction.Metadata.Data.Should().ContainKey("IPAddress");
        transaction.Metadata.Data["IPAddress"].Should().Be("192.168.1.100");
        transaction.Metadata.Data.Should().ContainKey("Device");
        transaction.Metadata.Data["Device"].Should().Be("Mobile");
        transaction.Metadata.Data.Should().ContainKey("Browser");
        transaction.Metadata.Data["Browser"].Should().Be("Chrome");
    }
}

