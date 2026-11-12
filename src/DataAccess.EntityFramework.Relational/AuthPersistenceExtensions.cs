using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Server.ClientAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

/// <summary>
/// Provides default stores while allowing applications to supply alternative OpenIddict and replay stores.
/// </summary>
public static class AuthPersistenceExtensions
{
    /// <summary>
    /// Adds the Basilisque-backed relational stores. Register Basilisque provider services in the host.
    /// </summary>
    public static IServiceCollection AddBaseAuthRelationalPersistence(this IServiceCollection services)
    {
        services.AddDbContext<AuthDbContext>();
        services.AddOpenIddict().AddCore(options =>
        {
            options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>();
        });
        services.TryAddScoped<IClientAssertionReplayStore, EfClientAssertionReplayStore>();
        return services;
    }

    /// <summary>
    /// Deletes expired replay markers. Run periodically using one scoped context per maintenance operation.
    /// </summary>
    public static Task<int> PruneExpiredAssertionsAsync(this AuthDbContext database, CancellationToken cancellationToken = default)
    {
        var cutoffUtcTicks = DateTimeOffset.UtcNow.UtcTicks;
        return database.AssertionReplays.Where(entry => entry.ExpiresAtUtcTicks < cutoffUtcTicks)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
