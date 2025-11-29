using FraudRuleEngine.Core.Domain;
using FraudRuleEngine.Core.Domain.DataRequests;
using FraudRuleEngine.Core.Domain.Rules;
using FraudRuleEngine.Evaluations.Worker.Data;
using FraudRuleEngine.Evaluations.Worker.Data.Repositories;
using FraudRuleEngine.Evaluations.Worker.Data.Requests;
using FraudRuleEngine.Evaluations.Worker.Services;
using FraudRuleEngine.Evaluations.Worker.Workers;
using FraudRuleEngine.Shared.Messaging;
using FraudRuleEngine.Shared.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<RulesEngineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FraudDb")));

// Repositories
builder.Services.AddScoped<IFraudCheckRepository, FraudCheckRepository>();
builder.Services.AddScoped<ITransactionHistoryRepository, TransactionHistoryRepository>();

// Data Request Handlers
builder.Services.AddScoped<IRequestHandler<RecentTransactionCountRequest, int>, RecentTransactionCountHandler>();

// Rule Data Mediator (dispatches requests to handlers)
builder.Services.AddScoped<IRuleDataContext, RuleDataMediator>();

// Rules
var fraudRulesConfig = builder.Configuration.GetSection("FraudRules");

builder.Services.AddScoped<IFraudRule, HighAmountRule>(sp =>
    new HighAmountRule(fraudRulesConfig.GetValue<decimal>("HighAmountRule:Threshold")));

builder.Services.AddScoped<IFraudRule, VelocityRule>(sp =>
    new VelocityRule(fraudRulesConfig.GetValue<int>("VelocityRule:MaxTransactionsPerHour")));

builder.Services.AddScoped<IFraudRule, ForeignCountryRule>(sp =>
    new ForeignCountryRule(fraudRulesConfig.GetValue<string>("ForeignCountryRule:AllowedCountry")));

// Rule Pipeline
builder.Services.AddScoped(sp =>
{
    var rules = sp.GetServices<IFraudRule>().ToList();
    return new CompositeRulePipeline(rules);
});

// Services
builder.Services.AddScoped<IFraudEvaluationService, FraudEvaluationService>();

// Kafka
builder.Services.AddScoped<IEventConsumer, KafkaEventConsumer>();
builder.Services.AddScoped<IEventProducer, KafkaEventProducer>();

// Worker
builder.Services.AddHostedService<FraudEvaluationWorker>();

// Resilience
builder.Services.AddHttpClient("HttpClient")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Initialize metrics early to ensure they're registered with the meter
_ = FraudMetrics.TransactionsReceivedTotal;
_ = FraudMetrics.FraudChecksTotal;
_ = FraudMetrics.TransactionsFlaggedTotal;
_ = FraudMetrics.RuleTriggersTotal;
_ = FraudMetrics.FraudRiskScore;
_ = FraudMetrics.FraudEvaluationDuration;
_ = FraudMetrics.ActiveFraudChecks;

// OpenTelemetry
var serviceName = "fraud-rule-engine-evaluations-worker";
var serviceVersion = "1.0.0";

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: serviceVersion));
    options.AddConsoleExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("FraudRuleEngine.Kafka.Consumer", "FraudRuleEngine.Kafka.Producer")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddRuntimeInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("FraudRuleEngine")
        .AddOtlpExporter(otlpOptions =>
        {
            // Push metrics to Prometheus OTLP receiver endpoint
            otlpOptions.Endpoint = new Uri(builder.Configuration["Prometheus:OtlpEndpoint"] ?? "http://prometheus:9090/api/v1/otlp/v1/metrics");
            otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }));

var host = builder.Build();

// Migrate database
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RulesEngineDbContext>();
    db.Database.Migrate();
}

host.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
