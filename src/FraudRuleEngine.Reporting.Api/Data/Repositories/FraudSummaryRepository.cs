using FraudRuleEngine.Reporting.Api.Data.Models;
using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Reporting.Api.Data.Repositories
{
    public class FraudSummaryRepository : IFraudSummaryRepository
    {
        private readonly FraudReportingDbContext _context;
        public FraudSummaryRepository(FraudReportingDbContext context)
        {
            _context = context;
        }

        public async Task<FraudSummary?> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken)
        {
            return await _context.FraudSummaries.AsNoTracking()
                .FirstOrDefaultAsync(s => s.TransactionId == transactionId, cancellationToken);
        }

        public async Task<DailyStatsDto?> GetDailyStatisticsAsync(DateTime date, CancellationToken cancellationToken)
        {
            var targetDate = date.Date;
            var nextDay = targetDate.AddDays(1);

            return await _context.FraudSummaries
                .AsNoTracking() 
                .Where(s => s.EvaluatedAt >= targetDate && s.EvaluatedAt < nextDay)
                .GroupBy(s => 1)
                .Select(g => new DailyStatsDto
                {
                    Date = targetDate,
                    TotalEvaluations = g.Count(),
                    FlaggedCount = g.Count(s => s.IsFlagged),
                    AverageRiskScore = g.Average(s => s.OverallRiskScore)
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}