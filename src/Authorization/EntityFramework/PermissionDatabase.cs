/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Microsoft.EntityFrameworkCore;
using System.Data;

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