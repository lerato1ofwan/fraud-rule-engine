using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FraudRuleEngine.Shared.Messaging;

public class KafkaEventProducer : IEventProducer, IDisposable
{
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

    public async Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            _logger.LogInformation("Message produced to topic {Topic} at offset {Offset}", topic, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic}", topic);
            throw;
        }
    }

    public async Task ProduceAsync(string topic, string jsonPayload, CancellationToken cancellationToken = default)
    {
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = jsonPayload
            };

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

