using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Basilisque.Base.Auth.Client.Credentials;

/// <summary>
/// Signing credentials with explicit ownership of their backing resource.
/// </summary>
public sealed class ClientSigningCredential : IDisposable
{
    private readonly IDisposable? _ownedResource;

    /// <summary>
    /// Wraps credentials. An optional resource is disposed with this wrapper.
    /// </summary>
    public ClientSigningCredential(SigningCredentials credentials, IDisposable? ownedResource = null)
    {
        Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _ownedResource = ownedResource;
    }

    /// <summary>
    /// Gets credentials for OpenIddict, including keys backed by a custom crypto provider.
    /// </summary>
    public SigningCredentials Credentials { get; }

    /// <summary>
    /// Uses an RSA private key with RS256. Ownership can be transferred to this wrapper.
    /// </summary>
    public static ClientSigningCredential FromRsa(RSA key, string keyId, bool ownsKey = false)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key.KeySize < 2048)
            throw new ArgumentException("RSA keys must contain at least 2048 bits.", nameof(key));

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException("A key identifier is required.", nameof(keyId));

        return new ClientSigningCredential(
            new SigningCredentials(new RsaSecurityKey(key) { KeyId = keyId }, SecurityAlgorithms.RsaSha256),
            ownsKey ? key : null);
    }

    /// <summary>
    /// Uses an RSA signing certificate. Ownership can be transferred to this wrapper.
    /// </summary>
    public static ClientSigningCredential FromCertificate(X509Certificate2 certificate, bool ownsCertificate = false)
    {
        if (certificate is null)
            throw new ArgumentNullException(nameof(certificate));

        if (!certificate.HasPrivateKey)
            throw new ArgumentException("The certificate has no private key.", nameof(certificate));

        using var key = certificate.GetRSAPrivateKey();
        if (key is null || key.KeySize < 2048)
            throw new ArgumentException("An RSA certificate with at least 2048 bits is required.", nameof(certificate));

        return new ClientSigningCredential(
            new SigningCredentials(new X509SecurityKey(certificate), SecurityAlgorithms.RsaSha256),
            ownsCertificate ? certificate : null);
    }

    /// <inheritdoc />
    public void Dispose() => _ownedResource?.Dispose();
}
