namespace Basilisque.Base.Auth.Authorization;

/// <summary>Manages the catalog, roles, and scoped assignments from trusted application code.</summary>
/// <remarks>The calling application authorizes administrative operations.</remarks>
public interface IPermissionManagement
{
    /// <summary>Updates the source and visibility hint of an action.</summary>
    Task SetActionMetadata(string actionId, string? sourceKey, bool hidden, CancellationToken cancellationToken = default);

    /// <summary>Updates the source and visibility hint of an object type.</summary>
    Task SetObjectTypeMetadata(string typeId, string? sourceKey, bool hidden, CancellationToken cancellationToken = default);

    /// <summary>Updates the source and visibility hint of an object.</summary>
    Task SetObjectMetadata(string objectId, string? sourceKey, bool hidden, CancellationToken cancellationToken = default);

    /// <summary>Sets or clears the display label of an existing action.</summary>
    Task SetActionDisplayName(string actionId, string? displayName, CancellationToken cancellationToken = default);

    /// <summary>Sets or clears the display label of an existing object type.</summary>
    Task SetObjectTypeDisplayName(string typeId, string? displayName, CancellationToken cancellationToken = default);

    /// <summary>Sets or clears the display label of an existing object.</summary>
    Task SetObjectDisplayName(string objectId, string? displayName, CancellationToken cancellationToken = default);

    /// <summary>Sets or clears the display label of an existing role.</summary>
    Task SetRoleDisplayName(string roleId, string? displayName, CancellationToken cancellationToken = default);

    /// <summary>Creates a type and defines its supported action IDs.</summary>
    Task CreateObjectType(PermissionObjectType objectType, CancellationToken cancellationToken = default);

    /// <summary>Replaces the supported actions of an existing object type.</summary>
    /// <remarks>Existing action metadata is preserved and unknown actions are added to the catalog.</remarks>
    /// <exception cref="InvalidOperationException">An explicit rule on an object of this type references an action being removed.</exception>
    Task SetObjectTypeActions(string typeId, IEnumerable<string> actionIds, CancellationToken cancellationToken = default);

    /// <summary>Creates an object under an optional existing parent. Use a new stable ID for each new object.</summary>
    Task CreateObject(PermissionObject permissionObject, CancellationToken cancellationToken = default);

    /// <summary>Changes the key of an existing object type while preserving its relationships.</summary>
    Task RenameObjectType(string key, string newKey, CancellationToken cancellationToken = default);

    /// <summary>Changes the key of an existing object while preserving its relationships.</summary>
    Task RenameObject(string key, string newKey, CancellationToken cancellationToken = default);

    /// <summary>Changes the key of an existing action while preserving its relationships.</summary>
    Task RenameAction(string key, string newKey, CancellationToken cancellationToken = default);

    /// <summary>Changes the key of an existing role while preserving its relationships.</summary>
    Task RenameRole(string key, string newKey, CancellationToken cancellationToken = default);


    /// <summary>Moves an existing object to a different parent, or to the root when the parent is null.</summary>
    Task MoveObject(string objectId, string? parentId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a leaf object and its explicit rules. Returns false if the object no longer exists.</summary>
    Task<bool> DeleteObject(string objectId, CancellationToken cancellationToken = default);

    /// <summary>Lists the current object types and permission objects.</summary>
    Task<PermissionCatalog> GetCatalog(CancellationToken cancellationToken = default);

    /// <summary>Creates a role or replaces its complete set of rules.</summary>
    Task SaveRole(PermissionRole role, CancellationToken cancellationToken = default);

    /// <summary>Lists the roles with their explicit rules.</summary>
    Task<IReadOnlyList<PermissionRole>> GetRoles(CancellationToken cancellationToken = default);

    /// <summary>Deletes a role and its assignments. Returns false if the role no longer exists.</summary>
    Task<bool> DeleteRole(string roleId, CancellationToken cancellationToken = default);

    /// <summary>Adds a scoped assignment and returns its ID for later revocation.</summary>
    Task<Guid> AssignRole(RoleAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>Lists the assignments of the given actor, including future and expired assignments.</summary>
    Task<IReadOnlyDictionary<Guid, RoleAssignment>> GetAssignments(string actorKind, string subjectId,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the assignment. Returns false if it no longer exists.</summary>
    Task<bool> RevokeAssignment(Guid assignmentId, CancellationToken cancellationToken = default);
}