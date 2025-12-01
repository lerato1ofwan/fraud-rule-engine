using FluentValidation;
using MediatR;
using FraudRuleEngine.Shared.Common;

namespace FraudRuleEngine.Transactions.Api.Services.Behaviours;

/// <summary>
/// MediatR pipeline behavior that validates requests using FluentValidation.
/// Returns validation errors before the handler is executed.
/// </summary>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    public ValidationBehaviour(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            var errorMessages = failures
                .Select(f => $"{f.PropertyName}: {f.ErrorMessage}")
                .ToList();

            _logger.LogWarning(
                "Validation failed for {RequestType}. Errors: {Errors}",
                typeof(TRequest).Name,
                string.Join("; ", errorMessages));

            // If TResponse is Result<T>, return a failure result
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(typeof(TResponse).GetGenericArguments()[0])
                    .GetMethod("Failure", new[] { typeof(string) });

                if (failureMethod != null)
                {
                    var combinedError = string.Join(" ", errorMessages);
                    return (TResponse)failureMethod.Invoke(null, new object[] { combinedError })!;
                }
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}

