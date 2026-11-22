namespace Basilisque.Base.Auth.Authorization;

/// <summary>Identifies an applicable direct or inherited role rule and the scopes activating it.</summary>
public sealed class PermissionRuleMatch
{
    /// <summary>Creates a matching rule and identifies the scopes in which its role applies.</summary>
    public PermissionRuleMatch(string roleId, RolePermissionRule rule, bool inherited, IEnumerable<string> scopeIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(scopeIds);

        RoleId = roleId;
        Rule = rule;
        Inherited = inherited;
        ScopeIds = Array.AsReadOnly(scopeIds.ToArray());
    }

    /// <summary>Gets the role that defines the rule.</summary>
    public string RoleId { get; }

    /// <summary>Gets the rule including its source object, action, and effect.</summary>
    public RolePermissionRule Rule { get; }

    /// <summary>Gets whether the rule came from an ancestor of the requested object.</summary>
    public bool Inherited { get; }

    /// <summary>Gets the scopes with active assignments for this role.</summary>
    public IReadOnlyList<string> ScopeIds { get; }
}
