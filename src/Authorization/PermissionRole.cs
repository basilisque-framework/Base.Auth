namespace Basilisque.Base.Auth.Authorization;

/// <summary>Groups permission rules into a reusable role.</summary>
public sealed class PermissionRole
{
    /// <summary>Creates a role with a stable identifier and its rules.</summary>
    public PermissionRole(string id, IEnumerable<RolePermissionRule> rules, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(rules);

        Id = id;
        DisplayName = displayName;
        Rules = Array.AsReadOnly(rules.ToArray());
    }

    /// <summary>Gets the stable role identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the optional label for display. Use the ID when the label is absent.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the rules defined by this role.</summary>
    public IReadOnlyList<RolePermissionRule> Rules { get; }
}
