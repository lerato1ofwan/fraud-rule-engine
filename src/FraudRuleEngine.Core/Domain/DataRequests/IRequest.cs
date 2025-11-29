namespace FraudRuleEngine.Core.Domain.DataRequests;

/// <summary>
/// Each rule can define specific data requirements as request objects.
/// </summary>
/// <typeparam name="TResponse">The type of data returned by this request</typeparam>
public interface IRequest<out TResponse>
{
    /// <summary>
    /// Unique identifier for this request type. Used for type-safe resolution.
    /// </summary>
    string RequestId { get; }
}

