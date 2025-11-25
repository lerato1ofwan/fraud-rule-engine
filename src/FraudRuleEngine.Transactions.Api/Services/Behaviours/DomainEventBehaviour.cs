using FraudRuleEngine.Transactions.Api.Data;
using FraudRuleEngine.Transactions.Api.Domain.Entities;
using FraudRuleEngine.Transactions.Api.Services.Messaging;
using MediatR;

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
}
