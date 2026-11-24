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

using Basilisque.Base.Auth.Authorization.EntityFramework;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Server.ClientAuth;
using Basilisque.Base.Auth.Server.Extensions;
using Basilisque.DataAccess.EntityFramework.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Integration.Tests;

/// <summary>Checks the real Basilisque provider-selection path without requiring external database engines.</summary>
[Category("Integration")]
public sealed class ProviderModelTests
{
    /// <summary>Produces provider-specific DDL including protocol stores and replay markers.</summary>
    [Test]
    [Arguments("SQLite")]
    [Arguments("PostgreSQL")]
    [Arguments("SqlServer")]
    public async Task GenerateCreateScript_ShouldIncludeAuthStores_InConfiguredProvider(string providerName)
    {
        // Arrange
        using var provider = createProvider(providerName);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        // Act
        var script = database.Database.GenerateCreateScript();
        // Assert
        await Assert.That(script.Contains("Auth_Applications", StringComparison.Ordinal)).IsTrue();
        await Assert.That(script.Contains("Auth_Authorizations", StringComparison.Ordinal)).IsTrue();
        await Assert.That(script.Contains("Auth_Scopes", StringComparison.Ordinal)).IsTrue();
        await Assert.That(script.Contains("Auth_Tokens", StringComparison.Ordinal)).IsTrue();
        await Assert.That(script.Contains("Auth_OpenIddict", StringComparison.Ordinal)).IsFalse();
        await Assert.That(script.Contains("Auth_AssertionReplays", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>Checks that each provider has a complete migration for its actual model.</summary>
    [Test]
    [Arguments("SQLite")]
    [Arguments("PostgreSQL")]
    [Arguments("SqlServer")]
    public async Task GenerateScript_ShouldIncludeInitialMigration_InConfiguredProvider(string providerName)
    {
        // Arrange
        using var provider = createProvider(providerName);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        // Act
        var script = database.GetService<IMigrator>().GenerateScript();
        // Assert
        await Assert.That(script.Contains("_Auth_InitialOpenIddict", StringComparison.Ordinal)).IsTrue();
        await Assert.That(script.Contains("Auth_Applications", StringComparison.Ordinal)).IsTrue();
        await Assert.That(script.Contains("__BASAuthMigrationsHistory", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>Selects the configured provider's migrations even when every provider module is registered.</summary>
    [Test]
    [Arguments("SQLite")]
    [Arguments("PostgreSQL")]
    [Arguments("SqlServer")]
    public async Task ResolveMigrationsAssembly_ShouldMatchDatabaseProvider_InCombinedHost(string providerName)
    {
        // Arrange
        using var provider = createProvider(providerName, registerAllProviders: true);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        // Act
        var assembly = database.GetService<IMigrationsAssembly>().Assembly.GetName().Name;
        // Assert
        await Assert.That(assembly).IsEqualTo("Basilisque.Base.Auth.DataAccess.EntityFramework." + providerName);
    }

    /// <summary>Checks the replacement initial snapshot against each provider's current model.</summary>
    [Test]
    [Arguments("SQLite")]
    [Arguments("PostgreSQL")]
    [Arguments("SqlServer")]
    public async Task HasPendingModelChanges_ShouldBeFalse_InCombinedHost(string providerName)
    {
        // Arrange
        using var provider = createProvider(providerName, registerAllProviders: true);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        // Act
        var pendingChanges = database.Database.HasPendingModelChanges();
        // Assert
        await Assert.That(pendingChanges).IsFalse();
    }

    /// <summary>Checks the independent Identity snapshot and generated assembly mapping for each provider.</summary>
    [Test]
    [Arguments("SQLite")]
    [Arguments("PostgreSQL")]
    [Arguments("SqlServer")]
    public async Task HasPendingModelChanges_ShouldBeFalse_ForStandardIdentityContext(string providerName)
    {
        // Arrange
        using var provider = createProvider(providerName, registerAllProviders: true, localUsers: true);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<LocalIdentityDbContext>();
        // Act
        var pendingChanges = database.Database.HasPendingModelChanges();
        // Assert
        await Assert.That(pendingChanges).IsFalse();
        await Assert.That(database.GetService<IMigrationsAssembly>().Assembly.GetName().Name)
            .IsEqualTo("Basilisque.Base.Auth.DataAccess.EntityFramework." + providerName);
        await Assert.That(database.Database.GetMigrations().Single().EndsWith("_Identity_InitialLocalIdentity", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>Checks that a machine-only host exposes only its protocol migration initializer.</summary>
    [Test]
    public async Task GetServices_ShouldReturnOnlyAuthInitializer_WhenLocalStoreIsDisabled()
    {
        // Arrange
        using var provider = createProvider("SQLite", registerAllProviders: true);
        using var scope = provider.CreateScope();
        // Act
        var initializers = scope.ServiceProvider.GetServices<IDbInitializer>().ToArray();
        // Assert
        await Assert.That(initializers.Length).IsEqualTo(1);
        await Assert.That(initializers[0].DbContext is AuthDbContext).IsTrue();
    }

    /// <summary>Checks that enabled Auth contexts record their migrations in the same history.</summary>
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MigrateDatabase_ShouldShareMigrationHistory_WhenOptionalStoresAreEnabled(bool permissions)
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "base-auth-migrations-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using var provider = createProvider("SQLite", registerAllProviders: true, localUsers: true,
                sqliteConnectionString: $"Data Source={path};Pooling=False", permissions: permissions);
            using var scope = provider.CreateScope();
            var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<LocalIdentityDbContext>();
            var initializers = scope.ServiceProvider.GetServices<IDbInitializer>().ToArray();
            var expectedMigrationIds = initializers.SelectMany(initializer => initializer.DbContext.Database.GetMigrations())
                .Order(StringComparer.Ordinal).ToArray();
            await auth.Database.MigrateAsync();
            auth.AssertionReplays.Add(new AssertionReplayEntry { Id = "existing", ExpiresAtUtcTicks = DateTimeOffset.UtcNow.AddHours(1).UtcTicks });
            await auth.SaveChangesAsync();
            // Act
            foreach (var initializer in initializers)
                initializer.MigrateDatabase();
            // Assert
            await Assert.That(initializers.Length).IsEqualTo(permissions ? 3 : 2);
            foreach (var initializer in initializers)
                await Assert.That(initializer.DbContext.Database.GetAppliedMigrations().SequenceEqual(expectedMigrationIds)).IsTrue();
            await Assert.That((await auth.AssertionReplays.AsNoTracking().SingleAsync()).Id).IsEqualTo("existing");
            await Assert.That(await identity.Users.CountAsync()).IsEqualTo(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>Applies the selected migrations to a real SQLite database with all modules registered.</summary>
    [Test]
    public async Task MigrateAsync_ShouldCreateAuthStores_InCombinedHost()
    {
        // Arrange
        using var provider = createProvider("SQLite", registerAllProviders: true);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await database.Database.OpenConnectionAsync();
        // Act
        await database.Database.MigrateAsync();
        // Assert
        await Assert.That(await database.AssertionReplays.CountAsync()).IsEqualTo(0);
        using var command = database.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('Auth_Applications', 'Auth_Authorizations', 'Auth_Scopes', 'Auth_Tokens')";
        await Assert.That(Convert.ToInt32(await command.ExecuteScalarAsync())).IsEqualTo(4);
    }

    /// <summary>Deletes expired replay markers while preserving markers that are still valid.</summary>
    [Test]
    public async Task PruneExpiredAssertionsAsync_ShouldPreserveUnexpiredMarkers_InSQLite()
    {
        // Arrange
        using var provider = createProvider("SQLite", registerAllProviders: true);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await database.Database.OpenConnectionAsync();
        await database.Database.MigrateAsync();
        database.AssertionReplays.AddRange(
            new AssertionReplayEntry { Id = "expired", ExpiresAtUtcTicks = DateTimeOffset.UtcNow.AddHours(-1).UtcTicks },
            new AssertionReplayEntry { Id = "valid", ExpiresAtUtcTicks = DateTimeOffset.UtcNow.AddHours(1).UtcTicks });
        await database.SaveChangesAsync();
        // Act
        var removed = await database.PruneExpiredAssertionsAsync();
        // Assert
        await Assert.That(removed).IsEqualTo(1);
        await Assert.That(await database.AssertionReplays.AsNoTracking().Select(entry => entry.Id).SingleAsync()).IsEqualTo("valid");
    }

    /// <summary>Checks that automatic pruning is throttled across replay-store scopes.</summary>
    [Test]
    public async Task TryUseAsync_ShouldPruneOnlyOnce_InTheSameIntervalAcrossScopes()
    {
        // Arrange
        using var provider = createProvider("SQLite",
            sqliteConnectionString: $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
        using var setupScope = provider.CreateScope();
        var database = setupScope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await database.Database.OpenConnectionAsync();
        await database.Database.MigrateAsync();
        await addExpiredReplayMarkerAsync(database, "expired-before-first-use");
        using var firstScope = provider.CreateScope();
        var expiration = DateTimeOffset.UtcNow.AddMinutes(5);
        await firstScope.ServiceProvider.GetRequiredService<IClientAssertionReplayStore>()
            .TryUseAsync("client", "first", expiration);
        await addExpiredReplayMarkerAsync(database, "expired-before-second-use");
        using var secondScope = provider.CreateScope();
        var secondStore = secondScope.ServiceProvider.GetRequiredService<IClientAssertionReplayStore>();

        // Act
        await secondStore.TryUseAsync("client", "second", expiration);

        // Assert
        await Assert.That(await database.AssertionReplays.AnyAsync(entry => entry.Id == "expired-before-first-use")).IsFalse();
        await Assert.That(await database.AssertionReplays.AnyAsync(entry => entry.Id == "expired-before-second-use")).IsTrue();
    }

    /// <summary>Checks that a failed prune does not invalidate a newly recorded replay marker.</summary>
    [Test]
    public async Task TryUseAsync_ShouldAcceptNewMarker_WhenPruningFails()
    {
        // Arrange
        using var provider = createProvider("SQLite");
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await database.Database.OpenConnectionAsync();
        await database.Database.MigrateAsync();
        await addExpiredReplayMarkerAsync(database, "expired");
        await database.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER RejectReplayPruning BEFORE DELETE ON Auth_AssertionReplays
            BEGIN
                SELECT RAISE(ABORT, 'Replay pruning rejected by test trigger');
            END;
            """);
        var store = scope.ServiceProvider.GetRequiredService<IClientAssertionReplayStore>();

        // Act
        var accepted = await store.TryUseAsync("client", "assertion", DateTimeOffset.UtcNow.AddMinutes(5));

        // Assert
        await Assert.That(accepted).IsTrue();
        await Assert.That(await database.AssertionReplays.CountAsync()).IsEqualTo(2);
    }

    private static async Task addExpiredReplayMarkerAsync(AuthDbContext database, string id)
    {
        database.AssertionReplays.Add(new AssertionReplayEntry
        {
            Id = id,
            ExpiresAtUtcTicks = DateTimeOffset.UtcNow.AddMinutes(-1).UtcTicks
        });
        await database.SaveChangesAsync();
    }

    /// <summary>Checks the permission migration mapping and snapshot for each configured provider.</summary>
    [Test]
    [Arguments("SQLite")]
    [Arguments("PostgreSQL")]
    [Arguments("SqlServer")]
    public async Task GetMigrations_ShouldUsePermissionSnapshot_WhenAuthorizationIsEnabled(string providerName)
    {
        // Arrange
        using var provider = createProvider(providerName, registerAllProviders: true, permissions: true);
        using var scope = provider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<PermissionDbContext>();

        // Act
        var migration = database.Database.GetMigrations().Single();

        // Assert
        await Assert.That(migration.EndsWith("_Permissions_Initial", StringComparison.Ordinal)).IsTrue();
        await Assert.That(database.Database.HasPendingModelChanges()).IsFalse();
    }

    private static ServiceProvider createProvider(string providerName, bool registerAllProviders = false,
        bool localUsers = false, string sqliteConnectionString = "Data Source=:memory:", bool permissions = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = providerName,
            ["Database:ConnectionStrings:SQLite:ConnectionString"] = sqliteConnectionString,
            ["Database:ConnectionStrings:PostgreSQL:ConnectionString"] = "Host=localhost;Database=auth;Username=design;Password=design",
            ["Database:ConnectionStrings:SqlServer:ConnectionString"] = "Server=localhost;Database=auth;Integrated Security=True;TrustServerCertificate=True"
        }).Build());
        if (registerAllProviders)
        {
            Basilisque.Base.Auth.DataAccess.EntityFramework.PostgreSQL.IServiceCollectionExtensions.RegisterServices(services);
            Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite.IServiceCollectionExtensions.RegisterServices(services);
            Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.IServiceCollectionExtensions.RegisterServices(services);
        }
        else
        {
            switch (providerName)
            {
                case "SQLite":
                    Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite.IServiceCollectionExtensions.RegisterServices(services);
                    break;
                case "PostgreSQL":
                    Basilisque.Base.Auth.DataAccess.EntityFramework.PostgreSQL.IServiceCollectionExtensions.RegisterServices(services);
                    break;
                case "SqlServer":
                    Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.IServiceCollectionExtensions.RegisterServices(services);
                    break;
                default:
                    throw new ArgumentException("Unsupported provider.");
            }
        }

        services.AddBaseAuthRelationalPersistence();
        if (permissions)
            services.AddBaseAuthRelationalAuthorization();
        if (localUsers)
            services.AddBaseAuthLocalUsers().AddBaseAuthRelationalUserStore();
        return services.BuildServiceProvider();
    }
}