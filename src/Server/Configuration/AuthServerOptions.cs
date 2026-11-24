namespace Basilisque.Base.Auth.Server.Configuration;

/// <summary>
/// Configures the initial machine-to-machine authorization server.
/// </summary>
public sealed class AuthServerOptions
{
    /// <summary>
    /// Gets or sets the canonical HTTPS issuer, including its deployment path base.
    /// </summary>
    public Uri Issuer { get; set; } = null!;

    /// <summary>
    /// Gets or sets the resource identifier accepted by the product API.
    /// </summary>
    public string Audience { get; set; } = "";

    /// <summary>
    /// Gets or sets the single API admission scope used by this initial M2M profile.
    /// </summary>
    public string ApiScope { get; set; } = "api";

    /// <summary>
    /// Gets or sets the token endpoint path relative to the application root.
    /// </summary>
    public string TokenEndpointPath { get; set; } = "/connect/token";

    /// <summary>
    /// Gets or sets the token lifetime; defaults to five minutes.
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(5);

    internal bool IsValid() => Issuer is { IsAbsoluteUri: true } && Issuer.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(Issuer.Query) && string.IsNullOrEmpty(Issuer.Fragment) && string.IsNullOrEmpty(Issuer.UserInfo) &&
        !string.IsNullOrWhiteSpace(Audience) && !string.IsNullOrWhiteSpace(ApiScope) && !ApiScope.Any(char.IsWhiteSpace) &&
        TokenEndpointPath.StartsWith("/", StringComparison.Ordinal) && !TokenEndpointPath.StartsWith("//", StringComparison.Ordinal) &&
        !TokenEndpointPath.Contains('?') && !TokenEndpointPath.Contains('#') &&
        AccessTokenLifetime > TimeSpan.Zero && AccessTokenLifetime <= TimeSpan.FromMinutes(15);
}