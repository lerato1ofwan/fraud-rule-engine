using FraudRuleEngine.Reporting.Api.Data;
using FraudRuleEngine.Reporting.Api.Data.Models;
using FraudRuleEngine.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FraudRuleEngine.Reporting.Api.Services.Projections;

public interface IFraudAssessedProjection
{
    Task ProjectAsync(FraudAssessed fraudAssessed, CancellationToken cancellationToken = default);
}

public class FraudAssessedProjection : IFraudAssessedProjection
{
    private readonly FraudReportingDbContext _context;
    private readonly ILogger<FraudAssessedProjection> _logger;

    public FraudAssessedProjection(
        FraudReportingDbContext context,
        ILogger<FraudAssessedProjection> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProjectAsync(FraudAssessed fraudAssessed, CancellationToken cancellationToken = default)
    {
        // Upsert fraud summary
        var summary = await _context.FraudSummaries
            .FirstOrDefaultAsync(s => s.TransactionId == fraudAssessed.TransactionId, cancellationToken);

        if (summary == null)
        {
            summary = new FraudSummary
            {
                TransactionId = fraudAssessed.TransactionId,
                FraudCheckId = fraudAssessed.FraudCheckId,
                IsFlagged = fraudAssessed.IsFlagged,
                OverallRiskScore = fraudAssessed.OverallRiskScore,
                EvaluatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            await _context.FraudSummaries.AddAsync(summary, cancellationToken);
        }
        else
        {
            summary.IsFlagged = fraudAssessed.IsFlagged;
            summary.OverallRiskScore = fraudAssessed.OverallRiskScore;
            summary.EvaluatedAt = DateTime.UtcNow;
        }

        // Update rule heatmap
        var today = DateTime.UtcNow.Date;
        foreach (var ruleResult in fraudAssessed.RuleResults.Where(r => r.Triggered))
        {
            var heatmap = await _context.FraudRuleHeatmaps
                .FirstOrDefaultAsync(
                    h => h.RuleName == ruleResult.RuleName && h.Date == today,
                    cancellationToken);

            if (heatmap == null)
            {
                heatmap = new FraudRuleHeatmap
                {
                    RuleName = ruleResult.RuleName,
                    Date = today,
                    TriggerCount = 1,
                    AverageRiskScore = ruleResult.RiskScore,
                    LastUpdated = DateTime.UtcNow
                };
                await _context.FraudRuleHeatmaps.AddAsync(heatmap, cancellationToken);
            }
            else
            {
                heatmap.TriggerCount++;
                heatmap.AverageRiskScore = (heatmap.AverageRiskScore * (heatmap.TriggerCount - 1) + ruleResult.RiskScore) / heatmap.TriggerCount;
                heatmap.LastUpdated = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Projected fraud assessment for transaction {TransactionId}", fraudAssessed.TransactionId);
    }
}

