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

using Basilisque.DataAccess.EntityFramework.Base.DependencyInjection;
using Basilisque.DataAccess.EntityFramework.Relational.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenIddict.EntityFrameworkCore.Models;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;

/// <summary>
/// Default persistent OpenIddict stores and cluster-wide assertion replay protection.
/// </summary>
public sealed class AuthDbContext : BaseDbContext<AuthDbContext>
{
    /// <summary>
    /// Creates the auth context using the configured Basilisque database provider.
    /// </summary>
    public AuthDbContext(IDbProviderServiceProvider provider)
        : base(provider)
    { }

    /// <summary>
    /// Gets replay markers. No assertions or private keys are stored here.
    /// </summary>
    public DbSet<AssertionReplayEntry> AssertionReplays => Set<AssertionReplayEntry>();

    /// <inheritdoc />
    protected override string GetAppAreaPrefix() => "Auth";

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        var relationalOptions = RelationalOptionsExtension.Extract(optionsBuilder.Options);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(
            relationalOptions.WithMigrationsHistoryTableName(AuthPersistenceExtensions.MigrationsHistoryTable)
            );

        optionsBuilder.UseOpenIddict();
    }

    /// <inheritdoc />
    protected override void OnBeforeModelCreating(ModelBuilder modelBuilder)
    {
        // Basilisque adds the Auth_ prefix after these explicit protocol-store mappings.
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreApplication>().ToTable("Applications");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreAuthorization>().ToTable("Authorizations");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreScope>().ToTable("Scopes");
        modelBuilder.Entity<OpenIddictEntityFrameworkCoreToken>().ToTable("Tokens");

        modelBuilder.Entity<AssertionReplayEntry>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).HasMaxLength(64);
            entity.HasIndex(entry => entry.ExpiresAtUtcTicks);
        });
    }
}