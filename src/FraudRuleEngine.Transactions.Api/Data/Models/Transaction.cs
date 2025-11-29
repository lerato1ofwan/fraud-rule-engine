using FraudRuleEngine.Shared.Events;
using FraudRuleEngine.Transactions.Api.Domain.Events;
using FraudRuleEngine.Transactions.Api.Domain.ValueObjects;
using FraudRuleEngine.Transactions.Api.Models;

namespace FraudRuleEngine.Transactions.Api.Domain.Entities;

public class Transaction : Entity
{
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public Guid MerchantId { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public string ExternalId { get; private set; } = string.Empty; // For idempotency (verify we don't process transactions twice)
    public TransactionMetadata Metadata { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Transaction() { } 

    private Transaction(
        Guid transactionId,
        Guid accountId,
        decimal amount,
        Guid merchantId,
        string currency,
        DateTime timestamp,
        string externalId,
        TransactionMetadata metadata)
    {
        TransactionId = transactionId;
        AccountId = accountId;
        Amount = amount;
        MerchantId = merchantId;
        Currency = currency;
        Timestamp = timestamp;
        ExternalId = externalId;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
    }

    public static Transaction Create(
        Guid accountId,
        decimal amount,
        Guid merchantId,
        string currency,
        DateTime timestamp,
        string externalId,
        Dictionary<string, string> metadata)
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            accountId,
            amount,
            merchantId,
            currency,
            timestamp,
            externalId,
            TransactionMetadata.Create(metadata));

        transaction.AddDomainEvent(new TransactionReceivedEvent(
            transaction.TransactionId,
            transaction.AccountId,
            transaction.Amount,
            transaction.MerchantId,
            transaction.Currency,
            transaction.Timestamp,
            transaction.Metadata.ToDictionary()));

        return transaction;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

