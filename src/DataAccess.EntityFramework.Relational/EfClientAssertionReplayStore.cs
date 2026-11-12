using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Server.ClientAuth;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

internal sealed class EfClientAssertionReplayStore : IClientAssertionReplayStore
{
    private readonly AuthDbContext _database;

    /// <summary>
    /// Creates a replay store using the scoped auth database context.
    /// </summary>
    public EfClientAssertionReplayStore(AuthDbContext database)
    {
        _database = database;
    }

    public async ValueTask<bool> TryUseAsync(string clientId, string assertionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientId + "\0" + assertionId)));
        var marker = new AssertionReplayEntry { Id = id, ExpiresAtUtcTicks = expiresAt.UtcTicks };
        _database.AssertionReplays.Add(marker);
        try
        {
            await _database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _database.Entry(marker).State = EntityState.Detached;
            // Distinguish a duplicate marker from an unavailable database or other persistence failure.
            if (await _database.AssertionReplays.AnyAsync(entry => entry.Id == id, cancellationToken))
                return false;
            throw;
        }
    }
}
