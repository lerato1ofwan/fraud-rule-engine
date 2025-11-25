using FraudRuleEngine.Reporting.Api.Data;
using FraudRuleEngine.Reporting.Api.Data.Repositories;
using FraudRuleEngine.Reporting.Api.Services.Projections;
using FraudRuleEngine.Reporting.Api.Services.Queries;
using FraudRuleEngine.Reporting.Api.Workers;
using FraudRuleEngine.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Fraud Rule Engine - Reporting API", Version = "v1" });
});

builder.Services.AddDbContext<FraudReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ReportingDb")));

builder.Services.AddScoped<IFraudSummaryRepository, FraudSummaryRepository>();
builder.Services.AddScoped<IFraudReportingRepository, FraudReportingRepository>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetFraudSummaryQuery).Assembly));

// Projections
builder.Services.AddScoped<IFraudAssessedProjection, FraudAssessedProjection>();

// Kafka
builder.Services.AddScoped<IEventConsumer, KafkaEventConsumer>();

// Worker
builder.Services.AddHostedService<FraudReportingWorker>();

// Resilience
builder.Services.AddHttpClient("HttpClient")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("ReportingDb") ?? string.Empty);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
app.MapControllers();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FraudReportingDbContext>();
    db.Database.Migrate();
}

app.Run();

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

