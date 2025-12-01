namespace FraudRuleEngine.Shared.Messaging;

/// <summary>
/// Centralized Kafka topic names used across the application.
/// </summary>
public static class KafkaTopics
{
    public const string TransactionReceived = "transaction.received";
    public const string FraudAssessed = "fraud.assessed";
    public const string DeadLetterQueue = "dlq";
}

