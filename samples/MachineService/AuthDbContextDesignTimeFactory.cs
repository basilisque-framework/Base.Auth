using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Base.Auth.MachineService;

/// <summary>Uses the same provider registrations for design-time migrations as the executable service.</summary>
public sealed class AuthDbContextDesignTimeFactory : BaseDesignTimeDbContextFactory<AuthDbContext>
{
    /// <inheritdoc />
    public override void ConfigureProviderServices(IServiceCollection services, string[] args, bool isChildFactory)
    {
        new Basilisque.DataAccess.EntityFramework.SQLite.Design.SQLiteBaseDesignTimeDbContextFactory<AuthDbContext>()
            .ConfigureProviderServices(services, args, isChildFactory: true);
        new Basilisque.DataAccess.EntityFramework.PostgreSQL.Design.PostgreSQLBaseDesignTimeDbContextFactory<AuthDbContext>()
            .ConfigureProviderServices(services, args, isChildFactory: true);
        new Basilisque.DataAccess.EntityFramework.SqlServer.Design.SqlServerBaseDesignTimeDbContextFactory<AuthDbContext>()
            .ConfigureProviderServices(services, args, isChildFactory: true);
    }

    /// <inheritdoc />
    protected override IConfiguration CreateConfiguration(IServiceProvider serviceProvider, string[] args) => CreateDatabaseConfiguration(args);

    internal static IConfiguration CreateDatabaseConfiguration(string[] args) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SQLite",
            ["Database:ConnectionStrings:SQLite:ConnectionString"] = "Data Source=auth-design.db"
        }).AddEnvironmentVariables().AddCommandLine(args).Build();

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string[] args)
    {
        services.AddLogging();
        services.RegisterServices();
        services.AddBaseAuthRelationalPersistence();
    }
}
