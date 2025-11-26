using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Evaluations.Worker.Data.Repositories;

namespace FraudRuleEngine.Evaluations.Worker.Data.Requests;

/// <summary>
/// Handler for RecentTransactionCountRequest. Fetches transaction count from the database.
/// </summary>
public class RecentTransactionCountHandler : IRequestHandler<RecentTransactionCountRequest, int>
{
    private readonly ITransactionHistoryRepository _historyRepository;

    public RecentTransactionCountHandler(ITransactionHistoryRepository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task<int> HandleAsync(RecentTransactionCountRequest request, CancellationToken cancellationToken = default)
    {
        return await _historyRepository.GetRecentTransactionsCountAsync(
            request.AccountId,
            request.Since,
            cancellationToken);
    }
}