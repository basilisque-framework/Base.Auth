namespace Basilisque.Base.Auth.Server.ClientAuth;

/// <summary>
/// Atomically records assertion identifiers across all authorization-server instances.
/// </summary>
public interface IClientAssertionReplayStore
{
    /// <summary>
    /// Returns true only for the first use of this client's assertion identifier.
    /// </summary>
    ValueTask<bool> TryUseAsync(string clientId, string assertionId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
}
