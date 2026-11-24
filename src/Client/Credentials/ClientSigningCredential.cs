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
    /// Exports the public RSA or ECDSA key for registration at the token issuer.
    /// </summary>
    /// <returns>A JWK with public key parameters and the signing credential's key ID and algorithm.</returns>
    /// <exception cref="NotSupportedException">The credential cannot be exported as a supported public JWK.</exception>
    /// <remarks>The signing key must expose its public parameters. The private parameters are never exported.</remarks>
    public JsonWebKey ExportPublicJwk()
    {
        var key = Credentials.Key;
        var jwk = key switch
        {
            RsaSecurityKey rsa => exportRsa(rsa.Rsa is { } rsaKey ? rsaKey.ExportParameters(false) : rsa.Parameters),
            ECDsaSecurityKey ec => exportEc(ec.ECDsa.ExportParameters(false)),
            X509SecurityKey certificate => exportCertificate(certificate.Certificate),
            _ => throw new NotSupportedException(Messages.UnsupportedSigningPublicKey)
        };
        jwk.Kid = key.KeyId;
        jwk.Alg = Credentials.Algorithm;
        jwk.Use = "sig";
        return jwk;
    }

    /// <summary>
    /// Uses an RSA private key with RS256. Ownership can be transferred to this wrapper.
    /// </summary>
    public static ClientSigningCredential FromRsa(RSA key, string keyId, bool ownsKey = false)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (key.KeySize < 2048)
            throw new ArgumentException(Messages.RsaKeyTooShort, nameof(key));

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException(Messages.KeyIdentifierRequired, nameof(keyId));

        return new ClientSigningCredential(
            new SigningCredentials(new RsaSecurityKey(key) { KeyId = keyId }, SecurityAlgorithms.RsaSha256),
            ownsKey ? key : null);
    }

    /// <summary>
    /// Uses an ECDSA private key with the signing algorithm corresponding to its named curve.
    /// Ownership can be transferred to this wrapper.
    /// </summary>
    /// <param name="key">An ECDSA signing key on P-256, P-384, or P-521.</param>
    /// <param name="keyId">The key identifier registered at the token issuer.</param>
    /// <param name="ownsKey">Whether disposing the credential also disposes the key.</param>
    /// <returns>Signing credentials for ES256, ES384, or ES512.</returns>
    public static ClientSigningCredential FromEcdsa(ECDsa key, string keyId, bool ownsKey = false)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException(Messages.KeyIdentifierRequired, nameof(keyId));

        var (_, algorithm) = getEcProfile(key.ExportParameters(false).Curve);
        return new ClientSigningCredential(
            new SigningCredentials(new ECDsaSecurityKey(key) { KeyId = keyId }, algorithm),
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
            throw new ArgumentException(Messages.CertificateHasNoPrivateKey, nameof(certificate));

        using var key = certificate.GetRSAPrivateKey();
        if (key is null || key.KeySize < 2048)
            throw new ArgumentException(Messages.RsaCertificateTooShort, nameof(certificate));

        return new ClientSigningCredential(
            new SigningCredentials(new X509SecurityKey(certificate), SecurityAlgorithms.RsaSha256),
            ownsCertificate ? certificate : null);
    }

    private static JsonWebKey exportCertificate(X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is not null)
            return exportRsa(rsa.ExportParameters(false));

        using var ec = certificate.GetECDsaPublicKey();
        if (ec is not null)
            return exportEc(ec.ExportParameters(false));

        throw new NotSupportedException(Messages.UnsupportedCertificateKey);
    }

    private static JsonWebKey exportRsa(RSAParameters parameters)
        => new()
        {
            Kty = JsonWebAlgorithmsKeyTypes.RSA,
            N = Base64UrlEncoder.Encode(parameters.Modulus),
            E = Base64UrlEncoder.Encode(parameters.Exponent)
        };

    private static JsonWebKey exportEc(ECParameters parameters)
    {
        var (curve, _) = getEcProfile(parameters.Curve);
        return new JsonWebKey
        {
            Kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
            Crv = curve,
            X = Base64UrlEncoder.Encode(parameters.Q.X),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y)
        };
    }

    private static (string Curve, string Algorithm) getEcProfile(ECCurve curve)
        => curve.Oid?.Value switch
        {
            "1.2.840.10045.3.1.7" => ("P-256", SecurityAlgorithms.EcdsaSha256),
            "1.3.132.0.34" => ("P-384", SecurityAlgorithms.EcdsaSha384),
            "1.3.132.0.35" => ("P-521", SecurityAlgorithms.EcdsaSha512),
            _ => throw new NotSupportedException(Messages.UnsupportedNamedCurve)
        };

    /// <inheritdoc />
    public void Dispose() => _ownedResource?.Dispose();
}