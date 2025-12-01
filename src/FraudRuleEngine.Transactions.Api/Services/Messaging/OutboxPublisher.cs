using FraudRuleEngine.Shared.Messaging;
using FraudRuleEngine.Transactions.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Transactions.Api.Services.Messaging;

public class OutboxPublisher : IHostedService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxPublisher> _logger;
    private Timer? _timer;
    private CancellationTokenSource? _stoppingCts;

    public OutboxPublisher(IServiceProvider serviceProvider, ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a token source that represents the service lifetime
        // This will be cancelled when StopAsync is called
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        _timer = new Timer(
            _ => 
            {
                // Fire and forget, but handle exceptions properly
                var task = PublishOutboxMessagesAsync(_stoppingCts!.Token);
                task.ContinueWith(
                    t => 
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            _logger.LogError(t.Exception.GetBaseException(), "Unhandled exception in OutboxPublisher timer callback");
                        }
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            },
            null,
            TimeSpan.Zero,
            PollingInterval);
        
        _logger.LogInformation("OutboxPublisher started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OutboxPublisher is stopping");
        _stoppingCts?.Cancel();
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _stoppingCts?.Dispose();
        return Task.CompletedTask;
    }

    private async Task PublishOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
            var producer = scope.ServiceProvider.GetRequiredService<IEventProducer>();

            var messages = await context.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (!messages.Any())
            {
                return; // No messages to process
            }

            var publishedCount = 0;

            foreach (var message in messages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var topic = GetTopicForEventType(message.EventType);
                    await producer.ProduceAsync(topic, message.Payload, cancellationToken);
                    message.ProcessedAt = DateTime.UtcNow;
                    publishedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                }
            }

            if (publishedCount > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully published and marked {Count} outbox messages as processed", publishedCount);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OutboxPublisher operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in OutboxPublisher");
        }
    }

    private static string GetTopicForEventType(string eventType)
    {
        return eventType switch
        {
            Shared.Events.EventTypes.TransactionReceived => KafkaTopics.TransactionReceived,
            _ => KafkaTopics.DeadLetterQueue
        };
    }
}

