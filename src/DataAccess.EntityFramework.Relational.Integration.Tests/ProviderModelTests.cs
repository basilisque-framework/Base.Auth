using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        await Assert.That(script.Contains("InitialOpenIddict", StringComparison.Ordinal)).IsTrue();
        await Assert.That(script.Contains("Auth_Applications", StringComparison.Ordinal)).IsTrue();
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

    private static ServiceProvider createProvider(string providerName, bool registerAllProviders = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = providerName,
            ["Database:ConnectionStrings:SQLite:ConnectionString"] = "Data Source=:memory:",
            ["Database:ConnectionStrings:PostgreSQL:ConnectionString"] = "Host=localhost;Database=auth;Username=design;Password=design",
            ["Database:ConnectionStrings:SqlServer:ConnectionString"] = "Server=localhost;Database=auth;Integrated Security=True;TrustServerCertificate=True"
        }).Build());
        if (registerAllProviders)
        {
            Basilisque.Base.Auth.DataAccess.EntityFramework.PostgreSQL.IServiceCollectionExtensions.RegisterServices(services);
            Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite.IServiceCollectionExtensions.RegisterServices(services);
            Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.IServiceCollectionExtensions.RegisterServices(services);
        }
        else switch (providerName)
        {
            case "SQLite": Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite.IServiceCollectionExtensions.RegisterServices(services); break;
            case "PostgreSQL": Basilisque.Base.Auth.DataAccess.EntityFramework.PostgreSQL.IServiceCollectionExtensions.RegisterServices(services); break;
            case "SqlServer": Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.IServiceCollectionExtensions.RegisterServices(services); break;
            default: throw new ArgumentException("Unsupported provider.");
        }
        services.AddBaseAuthRelationalPersistence();
        return services.BuildServiceProvider();
    }
}
