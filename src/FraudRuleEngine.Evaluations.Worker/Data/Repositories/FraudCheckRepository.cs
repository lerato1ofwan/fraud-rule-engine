using FraudRuleEngine.Evaluations.Worker.Data.Models;

namespace FraudRuleEngine.Evaluations.Worker.Data.Repositories;

public class FraudCheckRepository : IFraudCheckRepository
{
    private readonly RulesEngineDbContext _context;

    public FraudCheckRepository(RulesEngineDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FraudCheck fraudCheck, CancellationToken cancellationToken = default)
    {
        await _context.FraudChecks.AddAsync(fraudCheck, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

