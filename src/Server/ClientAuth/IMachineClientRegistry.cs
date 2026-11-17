using Microsoft.IdentityModel.Tokens;

namespace Basilisque.Base.Auth.Server.ClientAuth;

/// <summary>
/// Administrative provisioning operations; never expose these as anonymous enrollment endpoints.
/// </summary>
public interface IMachineClientRegistry
{
    /// <summary>
    /// Creates a client with a server-assigned subject and an optional context. Pass null for no context.
    /// Existing registrations are never overwritten.
    /// </summary>
    ValueTask RegisterAsync(string clientId, string? contextId, JsonWebKeySet publicKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces registered public keys, allowing an overlap during rotation. Keeps subject and context stable.
    /// </summary>
    ValueTask ReplacePublicKeysAsync(string clientId, JsonWebKeySet publicKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prevents future token acquisition. Previously issued JWTs remain valid until expiration.
    /// </summary>
    ValueTask DisableAsync(string clientId, CancellationToken cancellationToken = default);
}
