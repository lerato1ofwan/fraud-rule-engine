using FraudRuleEngine.Shared.Contracts;
using FraudRuleEngine.Shared.Messaging;
using FraudRuleEngine.Evaluations.Worker.Services;

namespace FraudRuleEngine.Evaluations.Worker.Workers;

public class FraudEvaluationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FraudEvaluationWorker> _logger;

    public FraudEvaluationWorker(
        IServiceProvider serviceProvider,
        ILogger<FraudEvaluationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fraud Evaluation Worker started");

        using var scope = _serviceProvider.CreateScope();
        var consumer = scope.ServiceProvider.GetRequiredService<IEventConsumer>();
        await consumer.ConsumeAsync<TransactionReceived>(
            KafkaTopics.TransactionReceived,
            async (message, ct) =>
            {
                var evaluationService = scope.ServiceProvider.GetRequiredService<IFraudEvaluationService>();
                var producer = scope.ServiceProvider.GetRequiredService<IEventProducer>();

                try
                {
                    _logger.LogInformation("Processing transaction {TransactionId}", message.TransactionId);

                    var fraudCheck = await evaluationService.EvaluateAsync(message, ct);

                    var fraudAssessed = new FraudAssessed
                    {
                        FraudCheckId = fraudCheck.FraudCheckId,
                        TransactionId = fraudCheck.TransactionId,
                        IsFlagged = fraudCheck.IsFlagged,
                        OverallRiskScore = fraudCheck.OverallRiskScore,
                        RuleResults = fraudCheck.RuleResults.Select(r => new RuleResult
                        {
                            RuleName = r.RuleName,
                            Triggered = r.Triggered,
                            RiskScore = r.RiskScore,
                            Reason = r.Reason
                        }).ToList()
                    };

                    await producer.ProduceAsync(KafkaTopics.FraudAssessed, fraudAssessed, ct);

                    _logger.LogInformation(
                        "Fraud evaluation completed for transaction {TransactionId}. Flagged: {IsFlagged}, Risk: {RiskScore}",
                        message.TransactionId,
                        fraudCheck.IsFlagged,
                        fraudCheck.OverallRiskScore);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing transaction {TransactionId}", message.TransactionId);
                    // Producer will automatically retry and publish to DLQ if all retries fail
                    await producer.ProduceAsync(KafkaTopics.DeadLetterQueue, message, ct);
                }
            },
            stoppingToken);
    }
}
