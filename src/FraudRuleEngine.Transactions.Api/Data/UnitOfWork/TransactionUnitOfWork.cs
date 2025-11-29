using FraudRuleEngine.Shared.Common;
using Microsoft.Extensions.Logging;

namespace FraudRuleEngine.Transactions.Api.Data.UnitOfWork;

public interface ITransactionUnitOfWork
{
    Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default);
}

public class TransactionUnitOfWork : ITransactionUnitOfWork
{
    private readonly TransactionDbContext _context;
    private readonly ILogger<TransactionUnitOfWork> _logger;

    public TransactionUnitOfWork(TransactionDbContext context, ILogger<TransactionUnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation(cancellationToken);

            if (result.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return result;
            }

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute unit of work for {OperationType}", typeof(T).Name);
            await transaction.RollbackAsync(cancellationToken);
            return Result<T>.Failure("Unable to persist the transaction. Please retry the request.");
        }
    }
}
