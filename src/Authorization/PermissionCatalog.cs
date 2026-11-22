namespace Basilisque.Base.Auth.Authorization;

/// <summary>Contains the permission actions, object types, and nodes available to the application.</summary>
public sealed class PermissionCatalog
{
    /// <summary>Creates a catalog from the given types and objects.</summary>
    public PermissionCatalog(IEnumerable<PermissionObjectType> objectTypes, IEnumerable<PermissionObject> objects,
        IEnumerable<PermissionAction>? actions = null)
    {
        ArgumentNullException.ThrowIfNull(objectTypes);
        ArgumentNullException.ThrowIfNull(objects);

        ObjectTypes = Array.AsReadOnly(objectTypes.ToArray());
        Objects = Array.AsReadOnly(objects.ToArray());
        Actions = Array.AsReadOnly((actions ?? ObjectTypes.SelectMany(type => type.ActionIds)
            .Distinct(StringComparer.Ordinal).Select(id => new PermissionAction(id))).ToArray());
    }

    /// <summary>Gets the types and their supported actions.</summary>
    public IReadOnlyList<PermissionObjectType> ObjectTypes { get; }

    /// <summary>Gets the nodes in the permission catalog.</summary>
    public IReadOnlyList<PermissionObject> Objects { get; }

    /// <summary>Gets the available actions and their display labels.</summary>
    public IReadOnlyList<PermissionAction> Actions { get; }
}
