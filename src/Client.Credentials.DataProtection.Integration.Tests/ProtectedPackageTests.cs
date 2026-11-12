using Basilisque.Base.Auth.Client.Credentials.DataProtection;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection.Integration.Tests;

/// <summary>Verifies protected packages are bound to the selected deployment and client context.</summary>
[Category("Integration")]
public sealed class ProtectedPackageTests : IDisposable
{
    private readonly RSA _key = RSA.Create(2048);
    private readonly DataProtectionPrivateKeyPackages _packages = new(new EphemeralDataProtectionProvider());

    /// <summary>Round trips a usable private key through deployment-managed protection.</summary>
    [Test]
    public async Task Load_ShouldRecoverSigningKey_InMatchingProtectionContext()
    {
        // Arrange
        var package = _packages.Protect(_key, "key-1", "product/client-1");
        // Act
        using var credential = _packages.Load(package, "product/client-1");
        // Assert
        await Assert.That(credential.Credentials.Key.KeyId).IsEqualTo("key-1");
        await Assert.That(package.Contains(Convert.ToBase64String(_key.ExportPkcs8PrivateKey()), StringComparison.Ordinal)).IsFalse();
    }

    /// <summary>Prevents importing a package under another client protection context.</summary>
    [Test]
    public async Task Load_ShouldRejectPackage_InDifferentProtectionContext()
    {
        // Arrange
        var package = _packages.Protect(_key, "key-1", "product/client-1");
        // Act / Assert
        await Assert.That(() => _packages.Load(package, "product/client-2")).Throws<CryptographicException>();
    }

    /// <summary>Rejects a package protected by another installation key ring.</summary>
    [Test]
    public async Task Load_ShouldRejectPackage_InDifferentDeployment()
    {
        // Arrange
        var package = _packages.Protect(_key, "key-1", "product/client-1");
        var other = new DataProtectionPrivateKeyPackages(new EphemeralDataProtectionProvider());
        // Act / Assert
        await Assert.That(() => other.Load(package, "product/client-1")).Throws<CryptographicException>();
    }

    /// <inheritdoc />
    public void Dispose() => _key.Dispose();
}
