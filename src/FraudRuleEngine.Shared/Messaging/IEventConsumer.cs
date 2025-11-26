namespace FraudRuleEngine.Shared.Messaging;

public interface IEventConsumer
{
    Task ConsumeAsync<T>(string topic, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class;
}

