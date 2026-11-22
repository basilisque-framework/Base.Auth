using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection;

/// <summary>
/// Uses ASP.NET Core Data Protection to protect and unprotect private-key packages for client credentials.
/// </summary>
/// <remarks>The host must persist and protect the Data Protection key ring and arrange cluster access.</remarks>
public sealed class DataProtectionPrivateKeyPackages : IPrivateKeyPackageProtector
{
    private const string Prefix = "baseauth-dp:v1:";
    private readonly IDataProtectionProvider _protectionProvider;

    /// <summary>
    /// Creates a private-key package protector using the host-managed Data Protection provider.
    /// </summary>
    public DataProtectionPrivateKeyPackages(IDataProtectionProvider protectionProvider)
    {
        _protectionProvider = protectionProvider;
    }

    /// <inheritdoc />
    public string Protect(RSA privateKey, string keyId, string protectionContext)
    {
        if (privateKey is null)
            throw new ArgumentNullException(nameof(privateKey));

        if (string.IsNullOrWhiteSpace(keyId))
            throw new ArgumentException(Messages.KeyIdentifierRequired, nameof(keyId));

        if (privateKey.KeySize < 2048 || keyId.Length > 200)
            throw new ArgumentException(Messages.InvalidKeyOrIdentifier);

        var material = exportPrivateKey(privateKey);

        byte[]? plaintext = null;
        using var stream = new MemoryStream();
        try
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            writer.Write(keyId);
            writer.Write(material.Length);
            writer.Write(material);

            writer.Flush();

            plaintext = stream.ToArray();

            return Prefix + Convert.ToBase64String(protector(protectionContext).Protect(plaintext));
        }
        finally
        {
            clear(material);

            if (plaintext is not null)
                clear(plaintext);

            clear(stream.GetBuffer());
        }
    }

    /// <inheritdoc />
    public ClientSigningCredential Load(string package, string protectionContext)
    {
        if (package is null)
            throw new ArgumentNullException(nameof(package));

        if (!package.StartsWith(Prefix, StringComparison.Ordinal) || package.Length > 100_000)
            throw new CryptographicException(Messages.UnsupportedPrivateKeyPackage);

        var plaintext = protector(protectionContext).Unprotect(Convert.FromBase64String(package.Substring(Prefix.Length)));

        RSA? key = null;
        try
        {
            using var stream = new MemoryStream(plaintext, writable: false);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            var keyId = reader.ReadString();
            var length = reader.ReadInt32();

            if (keyId.Length is 0 or > 200 || length <= 0 || length != stream.Length - stream.Position)
                throw new CryptographicException(Messages.InvalidPrivateKeyPackage);

            key = importPrivateKey(plaintext, (int)stream.Position, length);

            var credential = ClientSigningCredential.FromRsa(key, keyId, ownsKey: true);

            key = null;

            return credential;
        }
        finally
        {
            key?.Dispose();
            clear(plaintext);
        }
    }

    private IDataProtector protector(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException(Messages.ProtectionContextRequired, nameof(context));

        return _protectionProvider.CreateProtector("Basilisque.Base.Auth.Client.PrivateKeyPackage", "v1", context);
    }

    private static byte[] exportPrivateKey(RSA key)
    {
#if NET48
        // Framework RSA has no PKCS#8 export API. Delegate the encoding to Windows CNG.
        var parameters = key.ExportParameters(includePrivateParameters: true);
        try
        {
            using var exportKey = new RSACng();
            exportKey.ImportParameters(parameters);
            return exportKey.Key.Export(CngKeyBlobFormat.Pkcs8PrivateBlob);
        }
        finally
        {
            if (parameters.D is not null)
                clear(parameters.D);
            if (parameters.DP is not null)
                clear(parameters.DP);
            if (parameters.DQ is not null)
                clear(parameters.DQ);
            if (parameters.P is not null)
                clear(parameters.P);
            if (parameters.Q is not null)
                clear(parameters.Q);
            if (parameters.InverseQ is not null)
                clear(parameters.InverseQ);
        }
#else
        return key.ExportPkcs8PrivateKey();
#endif
    }

    private static RSA importPrivateKey(byte[] plaintext, int offset, int length)
    {
#if NET48
        var material = new byte[length];
        try
        {
            Buffer.BlockCopy(plaintext, offset, material, 0, length);
            using var key = CngKey.Import(material, CngKeyBlobFormat.Pkcs8PrivateBlob);
            return new RSACng(key);
        }
        finally { clear(material); }
#else
        var key = RSA.Create();
        try
        {
            key.ImportPkcs8PrivateKey(plaintext.AsSpan(offset, length), out var bytesRead);
            if (bytesRead != length) throw new CryptographicException(Messages.InvalidPrivateKeyPackage);
            return key;
        }
        catch { key.Dispose(); throw; }
#endif
    }

    private static void clear(byte[] buffer)
    {
#if NET48
        Array.Clear(buffer, 0, buffer.Length);
#else
        CryptographicOperations.ZeroMemory(buffer);
#endif
    }
}
