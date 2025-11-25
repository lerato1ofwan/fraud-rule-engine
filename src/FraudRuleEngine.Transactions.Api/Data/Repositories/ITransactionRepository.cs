using FraudRuleEngine.Transactions.Api.Domain.Entities;

namespace FraudRuleEngine.Transactions.Api.Data.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

