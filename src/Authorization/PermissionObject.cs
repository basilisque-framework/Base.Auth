namespace Basilisque.Base.Auth.Authorization;

/// <summary>Represents a node in the permission catalog.</summary>
public sealed class PermissionObject
{
    /// <summary>Creates a permission object with an optional parent.</summary>
    public PermissionObject(string id, string typeId, string? parentId = null, string? displayName = null, string? sourceKey = null, bool hidden = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeId);
        if (parentId is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(parentId);

        Id = id;
        TypeId = typeId;
        ParentId = parentId;
        DisplayName = displayName;
        SourceKey = sourceKey;
        Hidden = hidden;
    }

    /// <summary>Gets the stable object identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the optional label for display. Use the ID when the label is absent.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the optional key of the module that supplies this entry.</summary>
    public string? SourceKey { get; }

    /// <summary>Gets the visibility hint for the application.</summary>
    public bool Hidden { get; }

    /// <summary>Gets the identifier of the object type.</summary>
    public string TypeId { get; }

    /// <summary>Gets the identifier of the parent object, if any.</summary>
    public string? ParentId { get; }
}