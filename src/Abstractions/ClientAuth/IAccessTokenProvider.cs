namespace Basilisque.Base.Auth.ClientAuth;

/// <summary>
/// Obtains access tokens for explicitly configured technical clients.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets a usable token, renewing it when necessary. Never creates a user session.
    /// </summary>
    /// <remarks>Client configuration determines whether valid tokens are reused or every call performs a new exchange.</remarks>
    Task<AccessToken> GetAccessTokenAsync(string registrationName, CancellationToken cancellationToken = default);
}
