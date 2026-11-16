using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Server.ClientAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

internal sealed class EfClientAssertionReplayStore : IClientAssertionReplayStore
{
    private readonly AuthDbContext _database;
    private readonly PruningSchedule _pruningSchedule;
    private readonly ILogger<EfClientAssertionReplayStore> _logger;

    /// <summary>
    /// Creates a replay store using the scoped auth database context.
    /// </summary>
    public EfClientAssertionReplayStore(AuthDbContext database, PruningSchedule pruningSchedule,
        ILogger<EfClientAssertionReplayStore> logger)
    {
        _database = database;
        _pruningSchedule = pruningSchedule;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryUseAsync(string clientId, string assertionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientId + "\0" + assertionId)));
        var marker = new AssertionReplayEntry { Id = id, ExpiresAtUtcTicks = expiresAt.UtcTicks };
        _database.AssertionReplays.Add(marker);
        try
        {
            await _database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            _database.Entry(marker).State = EntityState.Detached;
            // Distinguish a duplicate marker from an unavailable database or other persistence failure.
            if (await _database.AssertionReplays.AnyAsync(entry => entry.Id == id, cancellationToken))
                return false;
            throw;
        }

        if (!cancellationToken.IsCancellationRequested && _pruningSchedule.TryStart())
        {
            try
            {
                await _database.PruneExpiredAssertionsAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The replay marker has already been recorded.
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to prune expired client assertion replay markers.");
            }
        }

        return true;
    }

    internal sealed class PruningSchedule
    {
        private const long IntervalMilliseconds = 60_000;
        private long _nextPruneAtMilliseconds;

        internal bool TryStart()
        {
            var now = Environment.TickCount64;
            var nextPruneAt = Volatile.Read(ref _nextPruneAtMilliseconds);
            return now >= nextPruneAt && Interlocked.CompareExchange(
                ref _nextPruneAtMilliseconds, now + IntervalMilliseconds, nextPruneAt) == nextPruneAt;
        }
    }
}
