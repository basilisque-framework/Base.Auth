using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.Authorization;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string AssignedRoleNotFound => get(nameof(AssignedRoleNotFound));

    internal static string AssignmentRequiresKnownRole => get(nameof(AssignmentRequiresKnownRole));

    internal static string CannotDeleteObjectWithChildren => get(nameof(CannotDeleteObjectWithChildren));

    internal static string ConflictingRoleId => get(nameof(ConflictingRoleId));

    internal static string DiagnosticsUnavailable => get(nameof(DiagnosticsUnavailable));

    internal static string DisplayNameTooLong(int value)
        => string.Format(CultureInfo.CurrentCulture, get(nameof(DisplayNameTooLong)), value);

    internal static string DuplicateRoleRule => get(nameof(DuplicateRoleRule));

    internal static string InvalidAssignmentPeriod => get(nameof(InvalidAssignmentPeriod));

    internal static string MoveWouldCreateCycle => get(nameof(MoveWouldCreateCycle));

    internal static string ObjectRequiresKnownType => get(nameof(ObjectRequiresKnownType));

    internal static string ObjectTypeNotFound => get(nameof(ObjectTypeNotFound));

    internal static string ParentRequiresKnownObject => get(nameof(ParentRequiresKnownObject));

    internal static string PermissionHierarchyCycle => get(nameof(PermissionHierarchyCycle));

    internal static string PermissionKeyNotFound => get(nameof(PermissionKeyNotFound));

    internal static string PermissionObjectNotFound => get(nameof(PermissionObjectNotFound));

    internal static string RemoveRulesBeforeDetachingActions => get(nameof(RemoveRulesBeforeDetachingActions));

    internal static string RuleRequiresSupportedAction => get(nameof(RuleRequiresSupportedAction));

    internal static string StringKeyTooLong(int value)
        => string.Format(CultureInfo.CurrentCulture, get(nameof(StringKeyTooLong)), value);

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;
}