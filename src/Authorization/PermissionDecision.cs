namespace Basilisque.Base.Auth.Authorization;

/// <summary>Contains a permission decision and the explicit rules that applied.</summary>
public sealed class PermissionDecision
{
    /// <summary>Creates a decision with its matching rules.</summary>
    public PermissionDecision(bool allowed, IEnumerable<PermissionRuleMatch> matchingRules)
    {
        ArgumentNullException.ThrowIfNull(matchingRules);
        Allowed = allowed;
        MatchingRules = Array.AsReadOnly(matchingRules.ToArray());
    }

    /// <summary>Gets whether the requested action is allowed.</summary>
    public bool Allowed { get; }

    /// <summary>Gets all matching rules. An empty list indicates that no rule applied.</summary>
    public IReadOnlyList<PermissionRuleMatch> MatchingRules { get; }
}