using FraudRuleEngine.Shared.Contracts;

namespace FraudRuleEngine.Core.Domain.DataRequests;

/// <summary>
/// Request for retrieving the count of recent transactions for an account within a time window.
/// Used by VelocityRule to check transaction velocity.
/// </summary>
public class RecentTransactionCountRequest : IRequest<int>
{
    public string RequestId => "RecentTransactionCount";
    
    public Guid AccountId { get; init; }
    public DateTime Since { get; init; }

    public RecentTransactionCountRequest(Guid accountId, DateTime since)
    {
        AccountId = accountId;
        Since = since;
    }

    /// <summary>
    /// Factory method to create a request from a transaction.
    /// </summary>
    public static RecentTransactionCountRequest FromTransaction(TransactionReceived transaction, TimeSpan timeWindow)
    {
        var since = (transaction.Timestamp == default ? DateTime.UtcNow : transaction.Timestamp) - timeWindow;
        return new RecentTransactionCountRequest(transaction.AccountId, since);
    }
}