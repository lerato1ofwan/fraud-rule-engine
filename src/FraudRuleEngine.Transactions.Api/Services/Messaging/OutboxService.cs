using FraudRuleEngine.Transactions.Api.Data;
using Newtonsoft.Json;
using System.Text.Json;

namespace FraudRuleEngine.Transactions.Api.Services.Messaging;

public interface IOutboxService
{
    Task AddToOutboxAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : class;
}

public class OutboxService : IOutboxService
{
    private readonly TransactionDbContext _context;

    public OutboxService(TransactionDbContext context)
    {
        _context = context;
    }

    public async Task AddToOutboxAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : class
    {
        var outboxMessage = new OutboxMessage
        {
            EventType = domainEvent.GetType().Name,
            Payload = JsonConvert.SerializeObject(domainEvent),
            CreatedAt = DateTime.UtcNow
        };

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}

