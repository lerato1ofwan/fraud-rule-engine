namespace FraudRuleEngine.Core.Domain.DataRequests;

/// <summary>
/// Provides a type-safe context for rules to access their required data.
/// Acts as a dispatcher for data requests.
/// </summary>
public interface IRuleDataContext
{
    /// <summary>
    /// Resolves a data request and returns the typed response.
    /// This method dispatches the request to the appropriate handler.
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="request">The data request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The requested data</returns>
    Task<TResponse> ResolveAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>;
}

