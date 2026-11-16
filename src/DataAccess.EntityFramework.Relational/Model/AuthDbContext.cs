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
