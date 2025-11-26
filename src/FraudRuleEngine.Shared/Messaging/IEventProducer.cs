namespace FraudRuleEngine.Shared.Messaging;

public interface IEventProducer
{
    Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class;
    Task ProduceAsync(string topic, string jsonPayload, CancellationToken cancellationToken = default);
}

