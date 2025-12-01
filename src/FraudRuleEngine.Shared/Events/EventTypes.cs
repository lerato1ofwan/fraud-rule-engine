namespace FraudRuleEngine.Shared.Events;

/// <summary>
/// Centralized event type names used for outbox pattern and system events.
/// </summary>
public static class EventTypes
{
    public const string TransactionReceived = "TransactionReceivedEvent";
    public const string FraudAssessed = "FraudAssessedEvent";
}

