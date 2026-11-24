using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.Client.Credentials.DataProtection.Compatibility;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string CompatibilityUsage => get(nameof(CompatibilityUsage));

    internal static string CrossFrameworkVerificationPassed => get(nameof(CrossFrameworkVerificationPassed));

    internal static string ProtectedPackageCreated => get(nameof(ProtectedPackageCreated));

    internal static string RecoveredKeyIdentifierChanged => get(nameof(RecoveredKeyIdentifierChanged));

    internal static string RecoveredKeyMismatch => get(nameof(RecoveredKeyMismatch));

    internal static string UnknownCompatibilityOperation => get(nameof(UnknownCompatibilityOperation));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}