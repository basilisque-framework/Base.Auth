using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string DesignTimeLifetimeAlreadyAssigned => get(nameof(DesignTimeLifetimeAlreadyAssigned));

    internal static string MissingIdentityMigrations(string value)
        => string.Format(CultureInfo.CurrentCulture, get(nameof(MissingIdentityMigrations)), value);

    internal static string ReplayMarkerPruningFailed => get(nameof(ReplayMarkerPruningFailed));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}
