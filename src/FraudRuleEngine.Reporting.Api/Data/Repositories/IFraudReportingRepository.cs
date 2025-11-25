using FraudRuleEngine.Reporting.Api.Domain.DTOs;

namespace FraudRuleEngine.Reporting.Api.Data.Repositories
{
    public interface IFraudReportingRepository
    {
        Task<List<TopRuleDto>?> GetTopRules(int top = 10, CancellationToken cancellationToken = default);
    }
}
