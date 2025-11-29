public interface ITransactionHistoryRepository
{
    Task<int> GetRecentTransactionsCountAsync(Guid accountId, DateTime since, CancellationToken cancellationToken = default);
}
