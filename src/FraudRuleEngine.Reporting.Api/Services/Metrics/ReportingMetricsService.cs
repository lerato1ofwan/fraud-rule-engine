using FraudRuleEngine.Reporting.Api.Data.Repositories;
using FraudRuleEngine.Reporting.Api.Metrics;

namespace FraudRuleEngine.Reporting.Api.Services.Metrics;

public class ReportingMetricsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportingMetricsService> _logger;
    private readonly TimeSpan _reconciliationInterval = TimeSpan.FromMinutes(15);

    public ReportingMetricsService(
        IServiceProvider serviceProvider,
        ILogger<ReportingMetricsService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reporting Metrics Service started");

        // Initialize metrics from database on startup
        await InitializeMetricsFromDatabaseAsync(stoppingToken);

        // Periodic reconciliation to handle special cases (service restarts, missed events, etc.)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_reconciliationInterval, stoppingToken);
                await ReconcileMetricsFromDatabaseAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling reporting metrics");
            }
        }
    }

    private async Task InitializeMetricsFromDatabaseAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var summaryRepository = scope.ServiceProvider.GetRequiredService<IFraudSummaryRepository>();
        
        var today = DateTime.UtcNow.Date;
        var dailyStats = await summaryRepository.GetDailyStatisticsAsync(today, cancellationToken);
        
        if (dailyStats != null)
        {
            ReportingMetrics.UpdateDailyStats(
                dailyStats.TotalEvaluations,
                dailyStats.FlaggedCount,
                (double)dailyStats.AverageRiskScore);
            
            _logger.LogInformation("Initialized reporting metrics from database. Evaluations: {Total}, Flagged: {Flagged}", 
                dailyStats.TotalEvaluations, 
                dailyStats.FlaggedCount);
        }
        else
        {
            ReportingMetrics.UpdateDailyStats(0, 0, 0);
            _logger.LogInformation("No daily stats found, initialized metrics to zero");
        }
    }

    private async Task ReconcileMetricsFromDatabaseAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var summaryRepository = scope.ServiceProvider.GetRequiredService<IFraudSummaryRepository>();
        
        var today = DateTime.UtcNow.Date;
        var dailyStats = await summaryRepository.GetDailyStatisticsAsync(today, cancellationToken);
        
        if (dailyStats != null)
        {
            var currentTotal = ReportingMetrics.GetDailyTotalEvaluations();
            var currentFlagged = ReportingMetrics.GetDailyFlaggedCount();
            
            // Only update if there's a significant discrepancy (handles edge cases)
            if (Math.Abs(currentTotal - dailyStats.TotalEvaluations) > 5 || 
                Math.Abs(currentFlagged - dailyStats.FlaggedCount) > 5)
            {
                _logger.LogWarning(
                    "Metrics discrepancy detected. In-memory: Total={InMemoryTotal}, Flagged={InMemoryFlagged}. DB: Total={DbTotal}, Flagged={DbFlagged}. Reconciling...",
                    currentTotal, currentFlagged, dailyStats.TotalEvaluations, dailyStats.FlaggedCount);
                
                ReportingMetrics.UpdateDailyStats(
                    dailyStats.TotalEvaluations,
                    dailyStats.FlaggedCount,
                    (double)dailyStats.AverageRiskScore);
            }
        }
    }
}

