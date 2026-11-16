using Basilisque.Base.Auth.Client.Credentials;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Basilisque.Base.Auth.Client.Unit.Tests.Credentials;

/// <summary>Checks the public key data used to provision machine clients.</summary>
[Category("Unit")]
public sealed class ClientSigningCredentialTests
{
    /// <summary>Exports the public RSA parameters and preserves the certificate key ID.</summary>
    [Test]
    public async Task ExportPublicJwk_ShouldExportOnlyPublicParameters_FromRsaCertificate()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Client Signing Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        using var credential = ClientSigningCredential.FromCertificate(certificate);
        var expected = rsa.ExportParameters(false);
        // Act
        var key = credential.ExportPublicJwk();
        // Assert
        await Assert.That(key.HasPrivateKey).IsFalse();
        await Assert.That(key.N).IsEqualTo(Base64UrlEncoder.Encode(expected.Modulus));
        await Assert.That(key.E).IsEqualTo(Base64UrlEncoder.Encode(expected.Exponent));
        await Assert.That(key.Kid).IsEqualTo(certificate.Thumbprint);
        await Assert.That(key.Alg).IsEqualTo(SecurityAlgorithms.RsaSha256);
    }
}
