using Basilisque.Base.Auth.Abstractions.Identity;
using Basilisque.Base.Auth.Authorization.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class EfPermissionEvaluator : IPermissionEvaluator, IPermissionDiagnostics
{
    private readonly PermissionDatabase _database;
    private readonly TimeProvider _timeProvider;

    public EfPermissionEvaluator(IDbContextFactory<PermissionDbContext> factory, TimeProvider timeProvider)
    {
        _database = new PermissionDatabase(factory);
        _timeProvider = timeProvider;
    }

    public async ValueTask<bool> IsAllowed(Actor actor, PermissionCheck check, CancellationToken cancellationToken = default)
    {
        var decisions = await AreAllowed(actor, new Dictionary<string, PermissionCheck> { [string.Empty] = check }, cancellationToken);
        return decisions[string.Empty];
    }

    public ValueTask<IReadOnlyDictionary<string, bool>> AreAllowed(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, CancellationToken cancellationToken = default)
        => evaluate(actor, checks, cancellationToken);

    public async ValueTask<IReadOnlyDictionary<string, PermissionDecision>> Explain(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, CancellationToken cancellationToken = default)
    {
        var matches = new Dictionary<string, List<PermissionRuleMatch>>(StringComparer.Ordinal);
        var decisions = await evaluate(actor, checks, cancellationToken, matches);
        return decisions.ToDictionary(pair => pair.Key, pair => new PermissionDecision(pair.Value,
            matches.TryGetValue(pair.Key, out var rules) ? rules : []), StringComparer.Ordinal).AsReadOnly();
    }

    private async ValueTask<IReadOnlyDictionary<string, bool>> evaluate(Actor actor,
        IReadOnlyDictionary<string, PermissionCheck> checks, CancellationToken cancellationToken,
        Dictionary<string, List<PermissionRuleMatch>>? explanations = null)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(checks);
        cancellationToken.ThrowIfCancellationRequested();

        var requests = new Dictionary<string, PermissionCheck>(checks, StringComparer.Ordinal);
        var scopes = new HashSet<string>(StringComparer.Ordinal);
        var actions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var check in requests.Values)
        {
            ArgumentNullException.ThrowIfNull(check);
            scopes.UnionWith(check.ApplicableScopeIds);
            actions.Add(check.ActionId);
        }

        if (scopes.Count == 0)
            return denyAll(requests);

        var now = _timeProvider.GetUtcNow();
        var utcNow = now.UtcDateTime;
        return await _database.InTransaction<IReadOnlyDictionary<string, bool>>(async database =>
        {
            explanations?.Clear();
            var candidateAssignments = await database.Assignments.AsNoTracking().Include(assignment => assignment.Role)
                .Where(assignment => assignment.ActorKind == actor.Kind && assignment.SubjectId == actor.SubjectId
                    && scopes.Contains(assignment.ScopeId)
                    && (assignment.ValidFromUtc == null || assignment.ValidFromUtc <= utcNow)
                    && (assignment.ValidUntilUtc == null || utcNow < assignment.ValidUntilUtc))
                .ToListAsync(cancellationToken);
            var assignments = candidateAssignments
                .Where(assignment => assignment.ActorKind == actor.Kind && assignment.SubjectId == actor.SubjectId
                    && scopes.Contains(assignment.ScopeId))
                .ToArray();
            if (assignments.Length == 0)
                return denyAll(requests);

            var rolesById = assignments.Select(assignment => assignment.Role).DistinctBy(role => role.Id)
                .ToDictionary(role => role.Id);
            var roleIds = rolesById.Keys.ToArray();
            var objects = await loadObjectPaths(database, requests.Values, cancellationToken);
            var objectIds = objects.Keys.ToArray();
            var typeIds = objects.Values.Select(permissionObject => permissionObject.TypeId).Distinct().ToArray();
            var types = await database.ObjectTypes.AsNoTracking().Include(type => type.Actions)
                .Where(type => typeIds.Contains(type.Id)).ToListAsync(cancellationToken);
            var rules = await database.Rules.AsNoTracking()
                .Include(rule => rule.Action)
                .Where(rule => roleIds.Contains(rule.RoleId) && actions.Contains(rule.Action.Key)
                    && objectIds.Contains(rule.ObjectId))
                .ToListAsync(cancellationToken);
            var rulesByRole = rules.Where(rule => actions.Contains(rule.Action.Key)).ToLookup(rule => rule.RoleId);
            var roles = rolesById.Values.Select(role => new PermissionRole(role.Key, rulesByRole[role.Id]
                .Select(rule => new RolePermissionRule(objects[rule.ObjectId].Key, rule.Action.Key, rule.Effect))));
            var typesById = types.ToDictionary(type => type.Id);
            var ruleEvaluator = new PermissionRuleEvaluator(types.Select(type => type.ToModel()),
                objects.Values.Select(item => new PermissionObject(item.Key, typesById[item.TypeId].Key,
                    item.ParentId is Guid parentId ? objects[parentId].Key : null)), roles,
                assignments.Select(assignment => assignment.ToModel()));

            return ruleEvaluator.Evaluate(actor, requests, now, cancellationToken, explanations);
        }, cancellationToken);
    }

    private static async Task<Dictionary<Guid, StoredObject>> loadObjectPaths(PermissionDbContext database,
        IEnumerable<PermissionCheck> checks, CancellationToken cancellationToken)
    {
        var keys = checks.Where(check => check.ApplicableScopeIds.Count > 0)
            .Select(check => check.ObjectId).ToHashSet(StringComparer.Ordinal);
        var targets = await database.Objects.AsNoTracking().Where(item => keys.Contains(item.Key))
            .ToListAsync(cancellationToken);
        var objects = targets.Where(item => keys.Contains(item.Key)).ToDictionary(item => item.Id);
        var pending = getMissingParents(objects.Values, objects);

        while (pending.Count > 0)
        {
            var level = await database.Objects.AsNoTracking().Where(candidate => pending.Contains(candidate.Id))
                .ToListAsync(cancellationToken);
            foreach (var permissionObject in level)
                objects.Add(permissionObject.Id, permissionObject);
            pending = getMissingParents(level, objects);
        }

        return objects;
    }

    private static HashSet<Guid> getMissingParents(IEnumerable<StoredObject> level, Dictionary<Guid, StoredObject> objects)
        => level.Select(item => item.ParentId).OfType<Guid>().Where(id => !objects.ContainsKey(id)).ToHashSet();

    private static IReadOnlyDictionary<string, bool> denyAll(IReadOnlyDictionary<string, PermissionCheck> checks)
        => checks.Keys.ToDictionary(key => key, _ => false, StringComparer.Ordinal).AsReadOnly();
}