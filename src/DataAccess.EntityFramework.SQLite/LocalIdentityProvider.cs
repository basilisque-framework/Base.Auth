using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.DependencyInjection.Registration.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite;

[RegisterServiceSingleton(As = typeof(ILocalIdentityProvider), ImplementsITypeName = false, Key = LocalIdentityProvider.Key)]
internal sealed class LocalIdentityProvider : ILocalIdentityProvider
{
    internal const string Key = "SQLite";

    string ILocalIdentityProvider.ProviderKey => Key;

    public void Configure(DbContextOptionsBuilder options, string connectionString, string migrationAssembly, string historyTable)
        => options.UseSqlite(connectionString, provider => provider
            .MigrationsAssembly(migrationAssembly)
            .MigrationsHistoryTable(historyTable));
}