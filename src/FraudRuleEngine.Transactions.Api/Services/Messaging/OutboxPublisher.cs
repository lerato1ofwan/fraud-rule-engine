using FraudRuleEngine.Shared.Messaging;
using FraudRuleEngine.Transactions.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Transactions.Api.Services.Messaging;

public class OutboxPublisher : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    private Timer? _timer;

    public OutboxPublisher(IServiceProvider serviceProvider, ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(async _ => await PublishOutboxMessages(cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task PublishOutboxMessages(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            var producer = scope.ServiceProvider.GetRequiredService<IEventProducer>();

            var messages = await context.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync();

            foreach (var message in messages)
            {
                try
                {
                    var topic = GetTopicForEventType(message.EventType);
                    await producer.ProduceAsync(topic, message.Payload, CancellationToken.None);
                    message.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                    // Consider: dead-letter queue or retry mechanism
                }
            }

            if (messages.Any())
            {
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // The timer will retry the operation after the specified interval
            _logger.LogError(ex, "Critical error in OutboxPublisher");
        }
    }

    private string GetTopicForEventType(string eventType)
    {
        return eventType switch
        {
            "TransactionReceivedEvent" => "transaction.received",
            _ => "dql"
        };
    }
}

