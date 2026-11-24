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
            ["Database:ConnectionStrings:SQLite:ConnectionString"] = "Data Source=auth-design.db",
            ["Database:ConnectionStrings:PostgreSQL:ConnectionString"] = "Host=localhost;Database=auth_design;Username=design",
            ["Database:ConnectionStrings:SqlServer:ConnectionString"] = "Server=localhost;Database=auth_design;Integrated Security=True"
        }).AddEnvironmentVariables().AddCommandLine(args).Build();

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string[] args)
    {
        services.AddLogging();
        services.RegisterServices();
        services.AddBaseAuthRelationalPersistence();
    }
}