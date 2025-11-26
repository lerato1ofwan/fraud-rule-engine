
using FraudRuleEngine.Evaluations.Worker.Data.Models;

public interface IFraudCheckRepository
{
    Task AddAsync(FraudCheck fraudCheck, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
