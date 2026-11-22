using System.Collections.Frozen;

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Identifies a business action and the scopes applicable to the target resource.</summary>
public sealed class PermissionCheck
{
    /// <summary>Creates a check with scope IDs resolved by the application from trusted resource data.</summary>
    public PermissionCheck(string objectId, string actionId, IEnumerable<string> applicableScopeIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        ArgumentNullException.ThrowIfNull(applicableScopeIds);

        ObjectId = objectId;
        ActionId = actionId;
        ApplicableScopeIds = applicableScopeIds.ToFrozenSet(StringComparer.Ordinal);
        foreach (var scopeId in ApplicableScopeIds)
            ArgumentException.ThrowIfNullOrWhiteSpace(scopeId, nameof(applicableScopeIds));
    }

    /// <summary>Gets the ID of the permission object.</summary>
    public string ObjectId { get; }

    /// <summary>Gets the stable ID of the requested action.</summary>
    public string ActionId { get; }

    /// <summary>Gets the set of applicable scope IDs. An empty set confers no role assignments.</summary>
    public IReadOnlySet<string> ApplicableScopeIds { get; }
}
