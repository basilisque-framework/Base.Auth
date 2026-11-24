using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.MachineClient;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string AuthorityAndApiRequireHttps => get(nameof(AuthorityAndApiRequireHttps));

    internal static string ClientCertificatePassword => get(nameof(ClientCertificatePassword));

    internal static string ExpectedCachedToken => get(nameof(ExpectedCachedToken));

    internal static string ExpectedServiceActor => get(nameof(ExpectedServiceActor));

    internal static string MachineClientFailed => get(nameof(MachineClientFailed));

    internal static string MachineClientUsage => get(nameof(MachineClientUsage));

    internal static string ServiceAuthenticated => get(nameof(ServiceAuthenticated));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}