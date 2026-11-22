using Basilisque.Base.Auth.Client.Credentials.DataProtection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection.Compatibility;

#if !NET48
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 4) throw new ArgumentException(Messages.CompatibilityUsage);
        var protection = DataProtectionProvider.Create(new DirectoryInfo(args[1]), options =>
            options.SetApplicationName("Base.Auth.Compatibility").ProtectKeysWithDpapi());
        var packages = new DataProtectionPrivateKeyPackages(protection);
        if (args[0] == "protect")
        {
            // Package provisioning starts with an exportable generated key, independently of certificate storage policy.
#if NET48
            using var key = new RSACryptoServiceProvider(2048) { PersistKeyInCsp = false };
#else
            using var key = RSA.Create(2048);
#endif
            File.WriteAllText(args[3], key.ToXmlString(includePrivateParameters: false));
            File.WriteAllText(args[2], packages.Protect(key, "compatibility-key", "compatibility/client"));
            Console.WriteLine(Messages.ProtectedPackageCreated);
            return 0;
        }
        if (args[0] != "verify") throw new ArgumentException(Messages.UnknownCompatibilityOperation);

        using var credential = packages.Load(File.ReadAllText(args[2]), "compatibility/client");
        var privateKey = (RsaSecurityKey)credential.Credentials.Key;
        var data = Encoding.UTF8.GetBytes("Base.Auth cross-framework signing check");
        var signature = privateKey.Rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var verifier = RSA.Create();
        verifier.FromXmlString(File.ReadAllText(args[3]));
        if (!verifier.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new CryptographicException(Messages.RecoveredKeyMismatch);
        if (credential.Credentials.Key.KeyId != "compatibility-key")
            throw new CryptographicException(Messages.RecoveredKeyIdentifierChanged);
        Console.WriteLine(Messages.CrossFrameworkVerificationPassed);
        return 0;
    }

}
