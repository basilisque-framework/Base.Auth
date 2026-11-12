using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.Integration.Tests;

/// <summary>Checks the SQL Server package carries the required provider integration.</summary>
[Category("Integration")]
public sealed class RegistrationTests
{
    /// <summary>Resolves a SQL Server configurator through the common Basilisque provider selector.</summary>
    [Test]
    public async Task RegisterServices_ShouldResolveConfigurator_InSqlServerConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer"
        }).Build());
        Basilisque.Base.Auth.DataAccess.EntityFramework.SqlServer.IServiceCollectionExtensions.RegisterServices(services);
        using var provider = services.BuildServiceProvider();
        // Act
        var configurator = provider.GetRequiredService<IDbProviderServiceProvider>().GetRequiredService<IDbContextOptionsConfigurator>();
        // Assert
        await Assert.That(configurator.GetType().Name).IsEqualTo("SqlServerDbContextOptionsConfigurator");
    }
}
