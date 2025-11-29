using FraudRuleEngine.Core.Domain.DataRequests;
using System.Collections.Concurrent;

namespace FraudRuleEngine.Evaluations.Worker.Services;

/// <summary>
/// Mediator implementation that dispatches data requests to their corresponding handlers.
/// MediatR's pattern inspired but adapted to our needs and our rules data requirements.
/// </summary>
public class RuleDataMediator : IRuleDataContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _handlerCache = new();

    public RuleDataMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> ResolveAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TRequest), typeof(TResponse));
        
        // Get or create handler instance (cached for performance)
        var handler = _handlerCache.GetOrAdd(handlerType, _ =>
        {
            var handlerInstance = _serviceProvider.GetService(handlerType);
            if (handlerInstance == null)
            {
                throw new InvalidOperationException(
                    $"No handler registered for request type {typeof(TRequest).Name}. " +
                    $"Please register an IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}> implementation.");
            }
            return handlerInstance;
        });

        // Invoke the handler using reflection
        var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<TRequest, TResponse>.HandleAsync));
        if (handleMethod == null)
        {
            throw new InvalidOperationException($"Handler for {typeof(TRequest).Name} does not implement HandleAsync method.");
        }

        var task = (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
        return await task;
    }
}

