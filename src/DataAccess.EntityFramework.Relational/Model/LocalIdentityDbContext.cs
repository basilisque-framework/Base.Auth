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

using Basilisque.Base.Auth.Server.Identity;
using Basilisque.DataAccess.EntityFramework.Base.Design;
using Basilisque.DataAccess.EntityFramework.Base.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;

/// <summary>
/// Stores local accounts using the ASP.NET Core Identity user model.
/// </summary>
public sealed class LocalIdentityDbContext : IdentityUserContext<LocalUser, Guid>, IInitializableDbContext, IDbContextDesignTimeLifetimeOwner
{
    private IDesignTimeServiceLifetime? _designTimeLifetime;
    private bool _disposed;

    /// <summary>
    /// Creates a local account context with the configured database provider.
    /// </summary>
    public LocalIdentityDbContext(DbContextOptions<LocalIdentityDbContext> options)
        : base(options)
    { }

    bool IInitializableDbContext.WasAlreadyMigrated { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LocalUser>(entity =>
        {
            entity.ToTable("Auth_Users");
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
            entity.Property(user => user.ContextId).HasMaxLength(256);
        });
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("Auth_UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("Auth_UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("Auth_UserTokens");

        if (builder.Model.FindEntityType(typeof(IdentityUserPasskey<Guid>)) is not null)
            builder.Entity<IdentityUserPasskey<Guid>>().ToTable("Auth_UserPasskeys");
    }

    void IDbContextDesignTimeLifetimeOwner.SetDesignTimeServiceLifetime(IDesignTimeServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (Interlocked.CompareExchange(ref _designTimeLifetime, lifetime, null) is not null)
            throw new InvalidOperationException(Messages.DesignTimeLifetimeAlreadyAssigned);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _disposed = true;

        var lifetime = Interlocked.Exchange(ref _designTimeLifetime, null);

        try
        {
            base.Dispose();
        }
        finally
        {
            lifetime?.Dispose();
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _disposed = true;
        var lifetime = Interlocked.Exchange(ref _designTimeLifetime, null);
        try
        {
            await base.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            if (lifetime is not null)
                await lifetime.DisposeAsync().ConfigureAwait(false);
        }
    }
}