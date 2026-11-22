using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string InvalidKeyOrIdentifier => get(nameof(InvalidKeyOrIdentifier));

    internal static string InvalidPrivateKeyPackage => get(nameof(InvalidPrivateKeyPackage));

    internal static string KeyIdentifierRequired => get(nameof(KeyIdentifierRequired));

    internal static string ProtectionContextRequired => get(nameof(ProtectionContextRequired));

    internal static string UnsupportedPrivateKeyPackage => get(nameof(UnsupportedPrivateKeyPackage));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}
