using FraudRuleEngine.Reporting.Api.Data.Models;
using FraudRuleEngine.Reporting.Api.Domain.DTOs;

namespace FraudRuleEngine.Reporting.Api.Data.Repositories
{
    public interface IFraudSummaryRepository
    {
        Task<FraudSummary?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken);
        Task<DailyStatsDto?> GetDailyStatisticsAsync(DateTime date, CancellationToken cancellationToken);
    }
}
