using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
using System.Diagnostics;
using System.Text;

namespace FraudRuleEngine.Shared.Messaging;

public class KafkaEventProducer : IEventProducer, IDisposable
{
    private static readonly ActivitySource ActivitySource = new("FraudRuleEngine.Kafka.Producer");
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public KafkaEventProducer(ILogger<KafkaEventProducer> logger, IConfiguration configuration)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 1 // Minimum required when idempotence is enabled
        };
        _producer = new ProducerBuilder<string, string>(config).Build();

        // Exponential backoff retry policy: 3 retries with 2 attempt seconds delay
        _retryPolicy = Policy
            .Handle<ProduceException<string, string>>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount}/3 for topic {Topic} after {Delay}s",
                        retryCount,
                        context.ContainsKey("topic") ? context["topic"] : "unknown",
                        timeSpan.TotalSeconds);
                });
    }

    public async Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonConvert.SerializeObject(message);
        await ProduceAsync(topic, json, cancellationToken);
    }

    private static void InjectTraceContext(Headers headers, string key, string value)
    {
        headers.Add(key, Encoding.UTF8.GetBytes(value));
    }

    public async Task ProduceAsync(string topic, string jsonPayload, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity($"kafka.produce.{topic}", ActivityKind.Producer);

        var context = new Context { ["topic"] = topic, ["payload"] = jsonPayload };

        try
        {
            await _retryPolicy.ExecuteAsync(async ctx =>
            {
                var kafkaMessage = new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = jsonPayload,
                    Headers = new Headers()
                };

                if (activity != null)
                {
                    activity.SetTag("messaging.system", "kafka");
                    activity.SetTag("messaging.destination", topic);
                    activity.SetTag("messaging.destination_kind", "topic");

                    Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), kafkaMessage.Headers, InjectTraceContext);
                }

                var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
                _logger.LogInformation("Message produced to topic {Topic} at offset {Offset}", topic, result.Offset);
            }, context);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic} after all retries", topic);
            await PublishToDeadLetterQueue(topic, jsonPayload, ex, cancellationToken);
        }
    }

    private async Task PublishToDeadLetterQueue(string originalTopic, string payload, Exception originalException, CancellationToken cancellationToken)
    {
        var dlqMessage = new
        {
            OriginalTopic = originalTopic,
            OriginalPayload = payload,
            FailureReason = originalException.Message,
            Timestamp = DateTime.UtcNow,
            ExceptionType = originalException.GetType().Name
        };
        var dlqPayload = JsonConvert.SerializeObject(dlqMessage);

        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = dlqPayload,
                Headers = new Headers()
            };

            // Add metadata headers
            kafkaMessage.Headers.Add("original-topic", Encoding.UTF8.GetBytes(originalTopic));
            kafkaMessage.Headers.Add("failure-reason", Encoding.UTF8.GetBytes(originalException.Message));
            kafkaMessage.Headers.Add("timestamp", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")));

            var result = await _producer.ProduceAsync(KafkaTopics.DeadLetterQueue, kafkaMessage, cancellationToken);
            _logger.LogWarning(
                "Message from topic {OriginalTopic} published to DLQ at offset {Offset}",
                originalTopic,
                result.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CRITICAL: Failed to publish message to DLQ. Message lost for topic {Topic}, Message: {Message}",
                originalTopic,
                dlqPayload);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}


