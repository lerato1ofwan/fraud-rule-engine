using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Evaluations.Worker.Data.Repositories;

public class TransactionHistoryRepository : ITransactionHistoryRepository
{
    private readonly RulesEngineDbContext _context;

    public TransactionHistoryRepository(RulesEngineDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetRecentTransactionsCountAsync(Guid accountId, DateTime since, CancellationToken cancellationToken = default)
    {
        // This would typically query a separate transaction history table
        // For now, we'll use fraud_checks as a proxy
        return await _context.FraudChecks
            .Where(fc => fc.AccountId == accountId && fc.EvaluatedAt >= since)
            .CountAsync(cancellationToken);
    }
}
