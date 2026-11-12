namespace Basilisque.Base.Auth.ClientAuth;

/// <summary>
/// An opaque bearer token and its server-reported expiration date.
/// </summary>
public sealed class AccessToken
{
    /// <summary>
    /// Gets the secret bearer token. Do not log this value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the expiration date when supplied by the authorization server.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Creates an access token. Tokens without expiration metadata are never cached.
    /// </summary>
    public AccessToken(string value, DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A token is required.", nameof(value));

        Value = value;
        ExpiresAt = expiresAt;
    }
}
