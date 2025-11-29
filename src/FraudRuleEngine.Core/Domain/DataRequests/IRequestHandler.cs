namespace FraudRuleEngine.Core.Domain.DataRequests;

/// <summary>
/// Handler interface for processing data requests.
/// Implementations reside in the service layer (Evaluations.Worker project) and handle data fetching.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the data request and returns the required data.
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

