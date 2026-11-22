using Basilisque.Base.Auth.Identity;

namespace Basilisque.Base.Auth.Authorization;

/// <summary>Applies role assignments and inherited rules to permission checks using already loaded data.</summary>
internal sealed class PermissionRuleEvaluator
{
    private readonly record struct IndexedRule
    {
        public string RoleId { get; }
        public PermissionEffect Effect { get; }

        public IndexedRule(string roleId, PermissionEffect effect)
        {
            RoleId = roleId;
            Effect = effect;
        }
    }

    private readonly Dictionary<string, PermissionObjectType> _objectTypes;
    private readonly Dictionary<string, PermissionObject> _objects;
    private readonly ILookup<(string ObjectId, string ActionId), IndexedRule> _rules;
    private readonly ILookup<(string ActorKind, string SubjectId), RoleAssignment> _assignments;

    /// <summary>Indexes the catalog, role rules, and scoped assignments for evaluation.</summary>
    /// <exception cref="ArgumentException">The supplied catalog, roles, or assignments are inconsistent.</exception>
    internal PermissionRuleEvaluator(IEnumerable<PermissionObjectType> objectTypes, IEnumerable<PermissionObject> objects,
        IEnumerable<PermissionRole> roles, IEnumerable<RoleAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(objectTypes);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(assignments);

        _objectTypes = objectTypes.ToDictionary(type => type.Id, StringComparer.Ordinal);
        _objects = objects.ToDictionary(permissionObject => permissionObject.Id, StringComparer.Ordinal);
        validateCatalog();
        var rolesById = roles.ToDictionary(role => role.Id, StringComparer.Ordinal);
        _rules = indexRules(rolesById.Values);
        _assignments = assignments.ToLookup(assignment => (assignment.ActorKind, assignment.SubjectId));
        if (_assignments.SelectMany(group => group).Any(assignment => !rolesById.ContainsKey(assignment.RoleId)))
            throw new ArgumentException(Messages.AssignmentRequiresKnownRole, nameof(assignments));
    }

    internal IReadOnlyDictionary<string, bool> Evaluate(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, DateTimeOffset at, CancellationToken cancellationToken = default,
        Dictionary<string, List<PermissionRuleMatch>>? explanations = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(checks);
        cancellationToken.ThrowIfCancellationRequested();

        var activeRoles = getActiveRoles(actor, at);
        var results = new Dictionary<string, bool>(checks.Count, StringComparer.Ordinal);
        foreach (var (key, check) in checks)
        {
            ArgumentNullException.ThrowIfNull(check);
            List<PermissionRuleMatch>? matches = null;
            if (explanations is not null)
            {
                matches = [];
                explanations.Add(key, matches);
            }
            results.Add(key, isAllowed(check, activeRoles, cancellationToken, matches));
        }

        return results.AsReadOnly();
    }

    private ILookup<string, string> getActiveRoles(Actor actor, DateTimeOffset at)
        => _assignments[(actor.Kind, actor.SubjectId)]
            .Where(assignment => (assignment.ValidFrom is null || assignment.ValidFrom <= at)
                && (assignment.ValidUntil is null || at < assignment.ValidUntil))
            .ToLookup(assignment => assignment.ScopeId, assignment => assignment.RoleId, StringComparer.Ordinal);

    private bool isAllowed(PermissionCheck check, ILookup<string, string> activeRoles, CancellationToken cancellationToken,
        List<PermissionRuleMatch>? matches)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_objects.TryGetValue(check.ObjectId, out var target) || !supportsAction(target, check.ActionId))
            return false;

        var applicableRoles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scopeId in check.ApplicableScopeIds)
            applicableRoles.UnionWith(activeRoles[scopeId]);
        if (applicableRoles.Count == 0)
            return false;

        var allowed = false;
        var denied = false;
        for (string? objectId = target.Id; objectId is not null; objectId = _objects[objectId].ParentId)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var rule in _rules[(objectId, check.ActionId)])
            {
                if (!applicableRoles.Contains(rule.RoleId))
                    continue;

                if (matches is not null)
                {
                    var ruleScopes = check.ApplicableScopeIds.Where(scope => activeRoles[scope].Contains(rule.RoleId));
                    matches.Add(new PermissionRuleMatch(rule.RoleId, new RolePermissionRule(objectId, check.ActionId, rule.Effect),
                        objectId != target.Id, ruleScopes));
                }
                else if (rule.Effect == PermissionEffect.Deny)
                    return false;

                allowed |= rule.Effect == PermissionEffect.Allow;
                denied |= rule.Effect == PermissionEffect.Deny;
            }
        }

        return allowed && !denied;
    }

    private bool supportsAction(PermissionObject permissionObject, string actionId)
        => _objectTypes[permissionObject.TypeId].ActionIds.Contains(actionId);

    private ILookup<(string ObjectId, string ActionId), IndexedRule> indexRules(IEnumerable<PermissionRole> roles)
    {
        var rules = new Dictionary<(string RoleId, string ObjectId, string ActionId), PermissionEffect>();
        foreach (var role in roles)
        {
            foreach (var rule in role.Rules)
            {
                if (!_objects.TryGetValue(rule.ObjectId, out var permissionObject) || !supportsAction(permissionObject, rule.ActionId))
                    throw new ArgumentException(Messages.RuleRequiresSupportedAction, nameof(roles));

                if (!rules.TryAdd((role.Id, rule.ObjectId, rule.ActionId), rule.Effect))
                    throw new ArgumentException(Messages.DuplicateRoleRule, nameof(roles));
            }
        }

        return rules.ToLookup(pair => (pair.Key.ObjectId, pair.Key.ActionId),
            pair => new IndexedRule(pair.Key.RoleId, pair.Value));
    }

    private void validateCatalog()
    {
        var validated = new HashSet<string>(StringComparer.Ordinal);
        var path = new HashSet<string>(StringComparer.Ordinal);
        foreach (var permissionObject in _objects.Values)
        {
            path.Clear();
            for (string? objectId = permissionObject.Id; objectId is not null && !validated.Contains(objectId);)
            {
                if (!path.Add(objectId))
                    throw new ArgumentException(Messages.PermissionHierarchyCycle);
                if (!_objects.TryGetValue(objectId, out var current))
                    throw new ArgumentException(Messages.ParentRequiresKnownObject);
                if (!_objectTypes.ContainsKey(current.TypeId))
                    throw new ArgumentException(Messages.ObjectRequiresKnownType);

                objectId = current.ParentId;
            }

            validated.UnionWith(path);
        }
    }
}
