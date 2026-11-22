using Microsoft.EntityFrameworkCore;

namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class EfPermissionManagement : IPermissionManagement
{
    private readonly IDbContextFactory<PermissionDbContext> _factory;
    private readonly PermissionDatabase _database;

    public EfPermissionManagement(IDbContextFactory<PermissionDbContext> factory)
    {
        _factory = factory;
        _database = new PermissionDatabase(factory);
    }

    public Task CreateObjectType(PermissionObjectType objectType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objectType);
        validateStringKey(objectType.Id);
        validateSourceKey(objectType.SourceKey);
        foreach (var actionId in objectType.ActionIds)
            validateStringKey(actionId);

        return _database.InTransaction(async database =>
        {
            var type = new StoredObjectType
            {
                Id = Guid.CreateVersion7(), Key = objectType.Id, DisplayName = normalizeDisplayName(objectType.DisplayName),
                SourceKey = objectType.SourceKey, Hidden = objectType.Hidden
            };
            await addMissingActions(database, type, objectType.ActionIds, cancellationToken);

            database.ObjectTypes.Add(type);
            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public Task SetObjectTypeActions(string typeId, IEnumerable<string> actionIds, CancellationToken cancellationToken = default)
    {
        validateStringKey(typeId);
        ArgumentNullException.ThrowIfNull(actionIds);
        var desired = actionIds.ToHashSet(StringComparer.Ordinal);
        foreach (var actionId in desired)
            validateStringKey(actionId);

        return _database.InTransaction(async database =>
        {
            var type = await database.ObjectTypes.Include(item => item.Actions)
                .SingleOrDefaultAsync(item => item.Key == typeId, cancellationToken);
            if (type?.Key != typeId)
                throw new ArgumentException(Messages.ObjectTypeNotFound, nameof(typeId));

            var removed = type.Actions.Where(action => !desired.Contains(action.Key)).Select(action => action.Id).ToArray();
            if (removed.Length > 0 && await database.Rules.AnyAsync(
                rule => rule.Object.TypeId == type.Id && removed.Contains(rule.ActionId), cancellationToken))
                throw new InvalidOperationException(Messages.RemoveRulesBeforeDetachingActions);

            type.Actions.RemoveAll(action => !desired.Contains(action.Key));
            await addMissingActions(database, type, desired, cancellationToken);
            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private static async Task addMissingActions(PermissionDbContext database, StoredObjectType type,
        IReadOnlySet<string> desired, CancellationToken cancellationToken)
    {
        var missing = desired.Except(type.Actions.Select(action => action.Key), StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        if (missing.Count == 0)
            return;

        var existing = await database.Actions.Where(action => missing.Contains(action.Key))
            .ToDictionaryAsync(action => action.Key, StringComparer.Ordinal, cancellationToken);
        foreach (var actionId in missing)
        {
            if (!existing.TryGetValue(actionId, out var action))
            {
                action = new StoredAction { Id = Guid.CreateVersion7(), Key = actionId };
                database.Actions.Add(action);
            }

            type.Actions.Add(action);
        }
    }

    public Task CreateObject(PermissionObject permissionObject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissionObject);
        validateStringKey(permissionObject.Id);
        validateStringKey(permissionObject.TypeId);
        validateSourceKey(permissionObject.SourceKey);

        return _database.InTransaction(async database =>
        {
            var type = await findByKey(database.ObjectTypes, permissionObject.TypeId, cancellationToken);
            if (type is null)
                throw new ArgumentException(Messages.ObjectTypeNotFound, nameof(permissionObject));

            var parent = permissionObject.ParentId is { } parentId
                ? await requireObject(database, parentId, cancellationToken) : null;

            database.Objects.Add(new StoredObject
            {
                Id = Guid.CreateVersion7(), Key = permissionObject.Id, TypeId = type.Id, ParentId = parent?.Id,
                DisplayName = normalizeDisplayName(permissionObject.DisplayName),
                SourceKey = permissionObject.SourceKey, Hidden = permissionObject.Hidden
            });
            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public Task RenameObjectType(string key, string newKey, CancellationToken cancellationToken = default)
        => rename<StoredObjectType>(key, newKey, cancellationToken);

    public Task RenameObject(string key, string newKey, CancellationToken cancellationToken = default)
        => rename<StoredObject>(key, newKey, cancellationToken);

    public Task RenameAction(string key, string newKey, CancellationToken cancellationToken = default)
        => rename<StoredAction>(key, newKey, cancellationToken);

    public Task RenameRole(string key, string newKey, CancellationToken cancellationToken = default)
        => rename<StoredRole>(key, newKey, cancellationToken);

    private Task rename<TEntity>(string key, string newKey, CancellationToken cancellationToken)
        where TEntity : class, IStoredPermissionKey
    {
        validateStringKey(key);
        validateStringKey(newKey);

        return _database.InTransaction(async database =>
        {
            var entity = await findByKey(database.Set<TEntity>(), key, cancellationToken)
                ?? throw new ArgumentException(Messages.PermissionKeyNotFound, nameof(key));
            entity.Key = newKey;
            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public Task SetActionMetadata(string actionId, string? sourceKey, bool hidden, CancellationToken cancellationToken = default)
        => setCatalogMetadata<StoredAction>(actionId, sourceKey, hidden, cancellationToken);

    public Task SetObjectTypeMetadata(string typeId, string? sourceKey, bool hidden, CancellationToken cancellationToken = default)
        => setCatalogMetadata<StoredObjectType>(typeId, sourceKey, hidden, cancellationToken);

    public Task SetObjectMetadata(string objectId, string? sourceKey, bool hidden, CancellationToken cancellationToken = default)
        => setCatalogMetadata<StoredObject>(objectId, sourceKey, hidden, cancellationToken);

    private Task setCatalogMetadata<TEntity>(string key, string? sourceKey, bool hidden, CancellationToken cancellationToken)
        where TEntity : class, IStoredCatalogEntry
    {
        validateStringKey(key);
        validateSourceKey(sourceKey);

        return _database.InTransaction(async database =>
        {
            var entity = await findByKey(database.Set<TEntity>(), key, cancellationToken)
                ?? throw new ArgumentException(Messages.PermissionKeyNotFound, nameof(key));
            entity.SourceKey = sourceKey;
            entity.Hidden = hidden;
            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private static void validateSourceKey(string? sourceKey)
    {
        if (sourceKey is not null)
            validateStringKey(sourceKey);
    }

    public Task SetActionDisplayName(string actionId, string? displayName, CancellationToken cancellationToken = default)
        => setDisplayName<StoredAction>(actionId, displayName, cancellationToken);

    public Task SetObjectTypeDisplayName(string typeId, string? displayName, CancellationToken cancellationToken = default)
        => setDisplayName<StoredObjectType>(typeId, displayName, cancellationToken);

    public Task SetObjectDisplayName(string objectId, string? displayName, CancellationToken cancellationToken = default)
        => setDisplayName<StoredObject>(objectId, displayName, cancellationToken);

    public Task SetRoleDisplayName(string roleId, string? displayName, CancellationToken cancellationToken = default)
        => setDisplayName<StoredRole>(roleId, displayName, cancellationToken);

    private Task setDisplayName<TEntity>(string key, string? displayName, CancellationToken cancellationToken)
        where TEntity : class, IStoredPermissionKey
    {
        validateStringKey(key);
        var label = normalizeDisplayName(displayName);
        return _database.InTransaction(async database =>
        {
            var entity = await findByKey(database.Set<TEntity>(), key, cancellationToken)
                ?? throw new ArgumentException(Messages.PermissionKeyNotFound, nameof(key));
            entity.DisplayName = label;
            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public Task MoveObject(string objectId, string? parentId, CancellationToken cancellationToken = default)
        => _database.InTransaction(async database =>
        {
            var permissionObject = await requireObject(database, objectId, cancellationToken);
            var parent = parentId is null ? null : await requireObject(database, parentId, cancellationToken);
            for (var ancestor = parent; ancestor is not null;)
            {
                if (ancestor.Id == permissionObject.Id)
                    throw new ArgumentException(Messages.MoveWouldCreateCycle, nameof(parentId));

                ancestor = ancestor.ParentId is Guid ancestorId
                    ? await database.Objects.FindAsync([ancestorId], cancellationToken) : null;
            }

            permissionObject.ParentId = parent?.Id;
            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

    public Task<bool> DeleteObject(string objectId, CancellationToken cancellationToken = default)
        => _database.InTransaction(async database =>
        {
            validateStringKey(objectId);
            var permissionObject = await findByKey(database.Objects, objectId, cancellationToken);
            if (permissionObject is null)
                return false;

            if (await database.Objects.AnyAsync(child => child.ParentId == permissionObject.Id, cancellationToken))
                throw new InvalidOperationException(Messages.CannotDeleteObjectWithChildren);

            database.Objects.Remove(permissionObject);
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

    public Task<PermissionCatalog> GetCatalog(CancellationToken cancellationToken = default)
        => _database.InTransaction(async database =>
        {
            var types = await database.ObjectTypes.AsNoTracking().Include(type => type.Actions).ToListAsync(cancellationToken);
            var objects = await database.Objects.AsNoTracking().Include(item => item.Type).Include(item => item.Parent)
                .ToListAsync(cancellationToken);
            var actions = await database.Actions.AsNoTracking().ToListAsync(cancellationToken);
            return new PermissionCatalog(types.Select(type => type.ToModel()), objects.Select(permissionObject => permissionObject.ToModel()),
                actions.Select(action => new PermissionAction(action.Key, action.DisplayName, action.SourceKey, action.Hidden)));
        }, cancellationToken);

    public Task SaveRole(PermissionRole role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        validateStringKey(role.Id);
        var displayName = normalizeDisplayName(role.DisplayName);
        var desired = role.Rules.ToDictionary(rule => (rule.ObjectId, rule.ActionId));
        foreach (var (objectId, actionId) in desired.Keys)
        {
            validateStringKey(objectId);
            validateStringKey(actionId);
        }

        return _database.InTransaction(async database =>
        {
            var resolved = new Dictionary<(Guid ObjectId, Guid ActionId), PermissionEffect>();
            if (desired.Count > 0)
            {
                var objectIds = desired.Keys.Select(key => key.ObjectId).ToHashSet(StringComparer.Ordinal);
                var catalog = await database.Objects.AsNoTracking().Where(item => objectIds.Contains(item.Key))
                    .Include(item => item.Type).ThenInclude(type => type.Actions)
                    .ToDictionaryAsync(item => item.Key, StringComparer.Ordinal, cancellationToken);
                foreach (var ((objectId, actionId), rule) in desired)
                {
                    var action = catalog.GetValueOrDefault(objectId)?.Type.Actions.SingleOrDefault(item => item.Key == actionId);
                    if (action is null)
                        throw new ArgumentException(Messages.RuleRequiresSupportedAction, nameof(role));

                    resolved.Add((catalog[objectId].Id, action.Id), rule.Effect);
                }
            }

            var storedRole = await database.Roles.Include(item => item.Rules)
                .SingleOrDefaultAsync(item => item.Key == role.Id, cancellationToken);
            if (storedRole is null)
            {
                storedRole = new StoredRole { Id = Guid.CreateVersion7(), Key = role.Id, DisplayName = displayName };
                database.Roles.Add(storedRole);
            }
            else if (storedRole.Key != role.Id)
            {
                throw new ArgumentException(Messages.ConflictingRoleId, nameof(role));
            }

            var existing = storedRole.Rules.ToDictionary(rule => (rule.ObjectId, rule.ActionId));
            if (role.DisplayName is not null)
                storedRole.DisplayName = displayName;
            foreach (var (key, rule) in existing)
            {
                if (!resolved.ContainsKey(key))
                    database.Rules.Remove(rule);
            }

            foreach (var (key, effect) in resolved)
            {
                if (existing.TryGetValue(key, out var storedRule))
                    storedRule.Effect = effect;
                else
                    storedRole.Rules.Add(new StoredRule
                    {
                        RoleId = storedRole.Id, ObjectId = key.ObjectId, ActionId = key.ActionId, Effect = effect
                    });
            }

            return await database.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionRole>> GetRoles(CancellationToken cancellationToken = default)
    {
        await using var database = await _factory.CreateDbContextAsync(cancellationToken);
        var roles = await database.Roles.AsNoTracking()
            .Include(role => role.Rules).ThenInclude(rule => rule.Object)
            .Include(role => role.Rules).ThenInclude(rule => rule.Action)
            .AsSingleQuery().ToListAsync(cancellationToken);
        return roles.Select(role => role.ToModel()).ToArray();
    }

    public Task<bool> DeleteRole(string roleId, CancellationToken cancellationToken = default)
        => _database.InTransaction(async database =>
        {
            validateStringKey(roleId);
            var role = await findByKey(database.Roles, roleId, cancellationToken);
            if (role is null)
                return false;

            database.Roles.Remove(role);
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }, cancellationToken);

    public Task<Guid> AssignRole(RoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        validateStringKey(assignment.RoleId);
        validateStringKey(assignment.ActorKind);
        validateStringKey(assignment.SubjectId);
        validateStringKey(assignment.ScopeId);

        var assignmentId = Guid.CreateVersion7();
        return _database.InTransaction(async database =>
        {
            if (await database.Assignments.AnyAsync(item => item.Id == assignmentId, cancellationToken))
                return assignmentId;

            var role = await findByKey(database.Roles, assignment.RoleId, cancellationToken);
            if (role is null)
                throw new ArgumentException(Messages.AssignedRoleNotFound, nameof(assignment));

            database.Assignments.Add(new StoredAssignment
            {
                Id = assignmentId, RoleId = role.Id, ActorKind = assignment.ActorKind,
                SubjectId = assignment.SubjectId, ScopeId = assignment.ScopeId,
                ValidFromUtc = assignment.ValidFrom?.UtcDateTime, ValidUntilUtc = assignment.ValidUntil?.UtcDateTime
            });
            await database.SaveChangesAsync(cancellationToken);
            return assignmentId;
        }, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, RoleAssignment>> GetAssignments(string actorKind, string subjectId,
        CancellationToken cancellationToken = default)
    {
        validateStringKey(actorKind);
        validateStringKey(subjectId);
        await using var database = await _factory.CreateDbContextAsync(cancellationToken);
        var assignments = await database.Assignments.AsNoTracking().Include(assignment => assignment.Role)
            .Where(assignment => assignment.ActorKind == actorKind && assignment.SubjectId == subjectId)
            .ToListAsync(cancellationToken);

        return assignments.Where(assignment => assignment.ActorKind == actorKind && assignment.SubjectId == subjectId)
            .ToDictionary(assignment => assignment.Id, assignment => assignment.ToModel()).AsReadOnly();
    }

    public async Task<bool> RevokeAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        await using var database = await _factory.CreateDbContextAsync(cancellationToken);
        return await database.Assignments.Where(assignment => assignment.Id == assignmentId)
            .ExecuteDeleteAsync(cancellationToken) != 0;
    }

    private static async Task<StoredObject> requireObject(PermissionDbContext database, string objectId,
        CancellationToken cancellationToken)
    {
        validateStringKey(objectId);
        return await findByKey(database.Objects, objectId, cancellationToken)
            ?? throw new ArgumentException(Messages.PermissionObjectNotFound, nameof(objectId));
    }

    private static async Task<TEntity?> findByKey<TEntity>(DbSet<TEntity> entities, string key, CancellationToken cancellationToken)
        where TEntity : class, IStoredPermissionKey
    {
        var entity = await entities.SingleOrDefaultAsync(item => EF.Property<string>(item, nameof(IStoredPermissionKey.Key)) == key,
            cancellationToken);
        return entity?.Key == key ? entity : null;
    }

    private static string? normalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        if (displayName.Length > PermissionDbContext.MaxDisplayNameLength)
            throw new ArgumentException(Messages.DisplayNameTooLong(PermissionDbContext.MaxDisplayNameLength), nameof(displayName));

        return displayName;
    }

    private static void validateStringKey(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (id.Length > PermissionDbContext.MaxStringKeyLength)
            throw new ArgumentException(Messages.StringKeyTooLong(PermissionDbContext.MaxStringKeyLength), nameof(id));
    }
}
