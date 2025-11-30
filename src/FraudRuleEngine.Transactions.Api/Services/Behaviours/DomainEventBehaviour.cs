using FraudRuleEngine.Transactions.Api.Data;
using FraudRuleEngine.Transactions.Api.Domain.Entities;
using FraudRuleEngine.Transactions.Api.Services.Messaging;
using FraudRuleEngine.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Transactions.Api.Services.Behaviors;

public class DomainEventBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOutboxService _outboxService;
    private readonly TransactionDbContext _context;

    public DomainEventBehaviour(IOutboxService outboxService, TransactionDbContext context)
    {
        _outboxService = outboxService;
        _context = context;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        // If the handler returned a failure result, don't process domain events
        if (response is Result<Guid> result && result.IsFailure)
        {
            return response;
        }

        try
        {
            // Collect domain events from entities
            var entitiesWithEvents = _context.ChangeTracker
                .Entries<Transaction>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();

            foreach (var entity in entitiesWithEvents)
            {
                foreach (var domainEvent in entity.DomainEvents)
                {
                    await _outboxService.AddToOutboxAsync(domainEvent, cancellationToken);
                }
                entity.ClearDomainEvents();
            }

            await _context.SaveChangesAsync(cancellationToken);

            return response;
        }
        catch (DbUpdateException dbEx)
        {
            // Handle database constraint violations and other DB errors
            var errorMessage = ExtractDatabaseErrorMessage(dbEx);
            
            // If TResponse is Result<T>, return a failure result
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(typeof(TResponse).GetGenericArguments()[0])
                    .GetMethod("Failure", new[] { typeof(string) });

                if (failureMethod != null)
                {
                    return (TResponse)failureMethod.Invoke(null, new object[] { errorMessage })!;
                }
            }

            throw;
        }
        catch (Exception ex)
        {
            // Handle any other exceptions that might occur during SaveChangesAsync
            // (e.g., InvalidOperationException from connection issues, etc.)
            // If SaveChangesAsync fails, nothing is persisted
            
            // If TResponse is Result<T>, return a failure result
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(typeof(TResponse).GetGenericArguments()[0])
                    .GetMethod("Failure", new[] { typeof(string) });

                if (failureMethod != null)
                {
                    // For non-DbUpdateException errors, use a generic error message
                    var errorMessage = "An error occurred while saving the transaction. Please verify your input and try again.";
                    
                    return (TResponse)failureMethod.Invoke(null, new object[] { errorMessage })!;
                }
            }

            throw;
        }
    }

    private static string ExtractDatabaseErrorMessage(Exception ex)
    {
        // Extract meaningful error message from database exceptions
        while (ex.InnerException != null)
        {
            ex = ex.InnerException;
        }

        // Handle PostgreSQL specific errors
        if (ex.Message.Contains("value too long for type character varying"))
        {
            if (ex.Message.Contains("varying(3)"))
            {
                return "Currency must be exactly 3 characters (ISO 4217 format).";
            }
            return "One or more fields exceed the maximum allowed length.";
        }

        if (ex.Message.Contains("duplicate key value"))
        {
            return "A record with this identifier already exists.";
        }

        if (ex.Message.Contains("violates not-null constraint"))
        {
            return "One or more required fields are missing.";
        }

        // Generic database error message
        return "An error occurred while saving the transaction. Please verify your input and try again.";
    }
}
