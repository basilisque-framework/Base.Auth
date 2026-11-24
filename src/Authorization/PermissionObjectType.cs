using System.Collections.Frozen;

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Defines the actions supported by a kind of permission object.</summary>
public sealed class PermissionObjectType
{
    /// <summary>Creates a type with the stable action IDs supported by its objects.</summary>
    public PermissionObjectType(string id, IEnumerable<string> actionIds, string? displayName = null, string? sourceKey = null, bool hidden = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(actionIds);

        Id = id;
        DisplayName = displayName;
        SourceKey = sourceKey;
        Hidden = hidden;
        ActionIds = actionIds.ToFrozenSet(StringComparer.Ordinal);
        foreach (var actionId in ActionIds)
            ArgumentException.ThrowIfNullOrWhiteSpace(actionId, nameof(actionIds));
    }

    /// <summary>Gets the stable type identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the optional label for display. Use the ID when the label is absent.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the optional key of the module that supplies this entry.</summary>
    public string? SourceKey { get; }

    /// <summary>Gets the visibility hint for the application.</summary>
    public bool Hidden { get; }

    /// <summary>Gets the action IDs supported by this type.</summary>
    public IReadOnlySet<string> ActionIds { get; }
}