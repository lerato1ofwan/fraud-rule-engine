using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace FraudRuleEngine.Shared.Messaging;

public class KafkaEventConsumer : IEventConsumer, IDisposable
{
    private static readonly ActivitySource ActivitySource = new("FraudRuleEngine.Kafka.Consumer");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaEventConsumer> _logger;

    public KafkaEventConsumer(ILogger<KafkaEventConsumer> logger, IConfiguration configuration)
    {
        _logger = logger;
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:GroupId"] ?? "default-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public async Task ConsumeAsync<T>(string topic, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class
    {
        _consumer.Subscribe(topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(cancellationToken);
                    if (result?.Message?.Value == null) continue;

                    // Extract trace context from Kafka headers
                    var parentContext = Propagator.Extract(default, result.Message.Headers, ExtractTraceContext);
                    Baggage.Current = parentContext.Baggage;

                    using var activity = ActivitySource.StartActivity($"kafka.consume.{result.Topic}", ActivityKind.Consumer, parentContext.ActivityContext);
                    
                    if (activity != null)
                    {
                        activity.SetTag("messaging.system", "kafka");
                        activity.SetTag("messaging.source", result.Topic);
                        activity.SetTag("messaging.source_kind", "topic");
                        activity.SetTag("messaging.kafka.partition", result.Partition);
                        activity.SetTag("messaging.kafka.offset", result.Offset);
                    }

                    var message = JsonSerializer.Deserialize<T>(result.Message.Value);
                    if (message != null)
                    {
                        await handler(message, cancellationToken);
                        _consumer.Commit(result);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from topic {Topic}", topic);
                    // backoff
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private static IEnumerable<string> ExtractTraceContext(Headers headers, string key)
    {
        var header = headers.FirstOrDefault(h => h.Key == key);
        return header != null ? new[] { Encoding.UTF8.GetString(header.GetValueBytes()) } : Enumerable.Empty<string>();
    }

    public void Dispose()
    {
        _consumer?.Dispose();
    }
}

