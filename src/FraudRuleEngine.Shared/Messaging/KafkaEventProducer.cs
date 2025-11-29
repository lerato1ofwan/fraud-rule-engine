using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FraudRuleEngine.Shared.Messaging;

public class KafkaEventProducer : IEventProducer, IDisposable
{
    private static readonly ActivitySource ActivitySource = new("FraudRuleEngine.Kafka.Producer");
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;

    public KafkaEventProducer(ILogger<KafkaEventProducer> logger, IConfiguration configuration)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    public async Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = ActivitySource.StartActivity($"kafka.produce.{topic}", ActivityKind.Producer);
        
        try
        {
            var json = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json,
                Headers = new Headers()
            };

            // Propagate trace context through Kafka headers
            if (activity != null)
            {
                activity.SetTag("messaging.system", "kafka");
                activity.SetTag("messaging.destination", topic);
                activity.SetTag("messaging.destination_kind", "topic");
                
                Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), kafkaMessage.Headers, InjectTraceContext);
            }

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            _logger.LogInformation("Message produced to topic {Topic} at offset {Offset}", topic, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic}", topic);
            throw;
        }
    }

    private static void InjectTraceContext(Headers headers, string key, string value)
    {
        headers.Add(key, Encoding.UTF8.GetBytes(value));
    }

    public async Task ProduceAsync(string topic, string jsonPayload, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity($"kafka.produce.{topic}", ActivityKind.Producer);
        
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = jsonPayload,
                Headers = new Headers()
            };

            // Propagate trace context through Kafka headers
            if (activity != null)
            {
                activity.SetTag("messaging.system", "kafka");
                activity.SetTag("messaging.destination", topic);
                activity.SetTag("messaging.destination_kind", "topic");
                
                Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), kafkaMessage.Headers, InjectTraceContext);
            }

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            _logger.LogInformation("Message produced to topic {Topic} at offset {Offset}", topic, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}

