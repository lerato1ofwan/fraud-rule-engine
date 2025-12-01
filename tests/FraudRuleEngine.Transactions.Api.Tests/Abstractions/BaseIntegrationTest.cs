using FraudRuleEngine.Transactions.Api.Data;
using FraudRuleEngine.Transactions.Api.Tests.Abstractions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FraudRuleEngine.Transactions.Api.Tests.Abstractions;

public class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{
    protected readonly ISender Sender;
    protected readonly TransactionDbContext DbContext;
    private readonly IServiceScope _scope;

    public BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        DbContext = _scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    }

    protected void CleanupDatabase()
    {
        DbContext.OutboxMessages.RemoveRange(DbContext.OutboxMessages);
        DbContext.TransactionIngestAudits.RemoveRange(DbContext.TransactionIngestAudits);
        DbContext.Transactions.RemoveRange(DbContext.Transactions);
        DbContext.SaveChanges();
    }
}

