using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class PermissionDatabase
{
    private readonly IDbContextFactory<PermissionDbContext> _factory;

    public PermissionDatabase(IDbContextFactory<PermissionDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<TResult> InTransaction<TResult>(Func<PermissionDbContext, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await using var strategyContext = await _factory.CreateDbContextAsync(cancellationToken);
        return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async token =>
        {
            await using var database = await _factory.CreateDbContextAsync(token);
            await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            var result = await operation(database);
            await transaction.CommitAsync(token);
            return result;
        }, cancellationToken);
    }
}
