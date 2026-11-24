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
using Basilisque.DataAccess.EntityFramework.Base.Connection;
using Basilisque.DataAccess.EntityFramework.Base.Data;
using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Basilisque.DataAccess.EntityFramework.Relational.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

/// <summary>
/// Provides default stores while allowing applications to supply alternative OpenIddict and replay stores.
/// </summary>
public static class AuthPersistenceExtensions
{
    internal const string MigrationsHistoryTable = "__BASAuthMigrationsHistory";

    /// <param name="identity">The builder for local Identity accounts.</param>
    extension(IdentityBuilder identity)
    {
        /// <summary>
        /// Uses the default relational store for local Identity accounts.
        /// </summary>
        public IdentityBuilder AddBaseAuthRelationalUserStore()
        {
            var services = identity.Services;
            services.Configure<IdentityOptions>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
            services.AddDbContext<LocalIdentityDbContext>((services, options) =>
            {
                var provider = services.GetRequiredService<IDbProviderServiceProvider>();
                var identityProvider = provider.GetRequiredService<ILocalIdentityProvider>();
                var providerKey = identityProvider.ProviderKey;
                var connection = provider.GetRequiredService<IConnectionStringBuilder>().GetConnectionString(nameof(AuthDbContext), providerKey);
                var migrationAssembly = services.GetRequiredService<IMigrationAssemblyResolver>()
                    .GetMigrationAssemblyName(typeof(LocalIdentityDbContext), providerKey)
                    ?? throw new InvalidOperationException(Messages.MissingIdentityMigrations(providerKey));

                identityProvider.Configure(options, connection, migrationAssembly, MigrationsHistoryTable);
            });
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDbInitializer, MigrationInitializer<LocalIdentityDbContext>>());
            return identity.AddEntityFrameworkStores<LocalIdentityDbContext>();
        }
    }

    /// <param name="services">The service collection that receives the relational store registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds persistent permission management and migrations for the configured Basilisque provider.
        ///  </summary>
        public IServiceCollection AddBaseAuthRelationalAuthorization()
        {
            PermissionServiceCollectionExtensions.AddPermissionServices(services);
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDbInitializer, MigrationInitializer<PermissionDbContext>>());
            return services;
        }

        /// <summary>
        /// Adds the Basilisque-backed relational stores. Register Basilisque provider services in the host.
        /// </summary>
        public IServiceCollection AddBaseAuthRelationalPersistence()
        {
            services.AddDbContext<AuthDbContext>();
            services.TryAddEnumerable(ServiceDescriptor.Scoped<IDbInitializer, MigrationInitializer<AuthDbContext>>());
            services.AddOpenIddict().AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>();
            });
            services.TryAddSingleton<EfClientAssertionReplayStore.PruningSchedule>();
            services.TryAddScoped<IClientAssertionReplayStore, EfClientAssertionReplayStore>();
            return services;
        }
    }

    /// <param name="database">The scoped authentication database context.</param>
    extension(AuthDbContext database)
    {
        /// <summary>
        /// Deletes expired replay markers on demand. Use a scoped context for standalone maintenance.
        /// </summary>
        public Task<int> PruneExpiredAssertionsAsync(CancellationToken cancellationToken = default)
        {
            var cutoffUtcTicks = DateTimeOffset.UtcNow.UtcTicks;
            return database.AssertionReplays.Where(entry => entry.ExpiresAtUtcTicks < cutoffUtcTicks)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    internal sealed class MigrationInitializer<TContext> : DbInitializer<TContext>
        where TContext : DbContext, IInitializableDbContext
    {
        public MigrationInitializer(TContext database) : base(database)
        {
        }
    }
}