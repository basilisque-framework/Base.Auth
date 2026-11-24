using System.Security.Cryptography;

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection;

/// <summary>
/// Protects private-key packages using deployment-managed protection keys.
/// </summary>
public interface IPrivateKeyPackageProtector
{
    /// <summary>
    /// Protects an RSA private key for a stable application/client-specific protection context.
    /// </summary>
    string Protect(RSA privateKey, string keyId, string protectionContext);

    /// <summary>
    /// Loads a credential from a protected package. The caller owns the returned credential.
    /// </summary>
    ClientSigningCredential Load(string package, string protectionContext);
}