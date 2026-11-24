using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Server.Extensions;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Base.Auth.MachineService;

/// <summary>Creates the local Identity context for EF migration tooling.</summary>
public sealed class LocalIdentityDbContextDesignTimeFactory : BaseDesignTimeDbContextFactory<LocalIdentityDbContext>
{
    /// <inheritdoc />
    protected override IConfiguration CreateConfiguration(IServiceProvider serviceProvider, string[] args) =>
        AuthDbContextDesignTimeFactory.CreateDatabaseConfiguration(args);

    /// <inheritdoc />
    protected override void ConfigureMigrationAssemblyProvider(IServiceCollection services, string[] args)
    {
        services.AddMigrationAssembly<LocalIdentityDbContext>("SQLite", typeof(DataAccess.EntityFramework.SQLite.DependencyRegistrator).Assembly);
        services.AddMigrationAssembly<LocalIdentityDbContext>("PostgreSQL", typeof(DataAccess.EntityFramework.PostgreSQL.DependencyRegistrator).Assembly);
        services.AddMigrationAssembly<LocalIdentityDbContext>("SqlServer", typeof(DataAccess.EntityFramework.SqlServer.DependencyRegistrator).Assembly);
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string[] args)
    {
        services.AddLogging();
        services.RegisterServices();
        services.AddBaseAuthLocalUsers().AddBaseAuthRelationalUserStore();
    }
}