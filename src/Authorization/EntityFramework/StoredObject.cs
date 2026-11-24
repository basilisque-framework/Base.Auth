namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class StoredObject : IStoredCatalogEntry
{
    public Guid Id { get; init; }
    public required string Key { get; set; }
    public string? DisplayName { get; set; }
    public string? SourceKey { get; set; }
    public bool Hidden { get; set; }
    public Guid TypeId { get; init; }
    public Guid? ParentId { get; set; }
    public StoredObjectType Type { get; init; } = null!;
    public StoredObject? Parent { get; init; }

    public PermissionObject ToModel() => new(Key, Type.Key, Parent?.Key, DisplayName, SourceKey, Hidden);
}