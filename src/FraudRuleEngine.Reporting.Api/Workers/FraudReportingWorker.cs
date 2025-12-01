using FraudRuleEngine.Reporting.Api.Services.Projections;
using FraudRuleEngine.Shared.Contracts;
using FraudRuleEngine.Shared.Messaging;

namespace FraudRuleEngine.Reporting.Api.Workers;

public class FraudReportingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FraudReportingWorker> _logger;

    public FraudReportingWorker(
        IServiceProvider serviceProvider,
        ILogger<FraudReportingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fraud Reporting Worker started");

        using var scope = _serviceProvider.CreateScope();
        var consumer = scope.ServiceProvider.GetRequiredService<IEventConsumer>();

        await consumer.ConsumeAsync<FraudAssessed>(
            KafkaTopics.FraudAssessed,
            async (message, ct) =>
            {
                var projection = scope.ServiceProvider.GetRequiredService<IFraudAssessedProjection>();

                try
                {
                    _logger.LogInformation("Processing fraud assessment for transaction {TransactionId}", message.TransactionId);
                    await projection.ProjectAsync(message, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing fraud assessment for transaction {TransactionId}", message.TransactionId);
                    throw;
                }
            },
            stoppingToken);
    }
}

