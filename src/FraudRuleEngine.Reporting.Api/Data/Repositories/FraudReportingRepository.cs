using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Reporting.Api.Data.Repositories
{
    public class FraudReportingRepository : IFraudReportingRepository
    {
        private readonly FraudReportingDbContext _context;

        public FraudReportingRepository(FraudReportingDbContext context)
        {
            _context = context;
        }

        public async Task<List<TopRuleDto>?> GetTopRules(int top = 10, CancellationToken cancellationToken = default)
        {
            return await _context.FraudRuleHeatmaps
                .OrderByDescending(r => r.TriggerCount)
                .Take(top)
                .Select(r => new TopRuleDto
                {
                    RuleName = r.RuleName,
                    TriggerCount = r.TriggerCount,
                    AverageRiskScore = r.AverageRiskScore,
                    Date = r.Date
                })
                .ToListAsync(cancellationToken);
        }
    }
}