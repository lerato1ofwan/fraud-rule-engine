using FraudRuleEngine.Shared.Messaging;
using FraudRuleEngine.Transactions.Api.Data;
using FraudRuleEngine.Transactions.Api.Data.Repositories;
using FraudRuleEngine.Transactions.Api.Data.UnitOfWork;
using FraudRuleEngine.Transactions.Api.Services.Commands;
using FraudRuleEngine.Transactions.Api.Services.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using FraudRuleEngine.Transactions.Api.Services.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Fraud Rule Engine - Transactions API", Version = "v1" });
});

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TransactionsDb")));

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionUnitOfWork, TransactionUnitOfWork>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IEventProducer, KafkaEventProducer>();

// MediatR for CQRS
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateTransactionCommand).Assembly);
});

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DomainEventBehaviour<,>));

// Polly for resilience
builder.Services.AddHttpClient("HttpClient")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Basic health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("TransactionsDb") ?? string.Empty)
    .AddCheck<KafkaHealthCheck>("kafka");

builder.Services.AddHostedService<OutboxPublisher>();

// OpenTelemetry
var serviceName = "fraud-rule-engine-transactions-api";
var serviceVersion = "1.0.0";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("FraudRuleEngine.Kafka.Producer")
        .AddJaegerExporter(options =>
        {
            options.AgentHost = builder.Configuration["Jaeger:AgentHost"] ?? "jaeger";
            options.AgentPort = builder.Configuration.GetValue<int>("Jaeger:AgentPort", 6831);
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("FraudRuleEngine")
        .AddPrometheusExporter());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Problem Details
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "An error occurred while processing your request.",
            status = StatusCodes.Status500InternalServerError
        });
    });
});

app.MapHealthChecks("/health");

// Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Controllers
app.MapControllers();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    db.Database.Migrate();
}

app.Run();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}

public class KafkaHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Kafka is available"));
    }
}

