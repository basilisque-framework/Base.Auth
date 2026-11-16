using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Basilisque.Base.Auth.Client.Credentials;

/// <summary>
/// Loads an explicitly installed client signing certificate.
/// </summary>
public static class CertificateStoreCredentials
{
    /// <summary>
    /// Loads one currently valid RSA certificate by thumbprint. The service account needs private-key access.
    /// </summary>
    public static ClientSigningCredential Load(string thumbprint, StoreLocation location = StoreLocation.CurrentUser)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
            throw new ArgumentException("A thumbprint is required.", nameof(thumbprint));

        using var store = new X509Store(StoreName.My, location);
        store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
        var certificates = store.Certificates;

        X509Certificate2? selected = null;
        X509Certificate2? retained = null;
        try
        {
            foreach (var certificate in certificates)
            {
                if (!string.Equals(certificate.Thumbprint, thumbprint.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (selected is not null)
                    throw new CryptographicException("Multiple matching signing certificates were found.");

                selected = certificate;
            }

            if (selected is null || DateTime.UtcNow < selected.NotBefore.ToUniversalTime() ||
                DateTime.UtcNow >= selected.NotAfter.ToUniversalTime())
                throw new CryptographicException("No currently valid signing certificate was found.");

            var result = ClientSigningCredential.FromCertificate(selected, ownsCertificate: true);
            retained = selected; // Ownership transfers to the returned wrapper.
            return result;
        }
        finally
        {
            foreach (var certificate in certificates)
                if (!ReferenceEquals(certificate, retained))
                    certificate.Dispose();
        }
    }
}
