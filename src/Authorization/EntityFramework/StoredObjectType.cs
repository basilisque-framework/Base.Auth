namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class StoredObjectType : IStoredCatalogEntry
{
    public Guid Id { get; init; }
    public required string Key { get; set; }
    public string? DisplayName { get; set; }
    public string? SourceKey { get; set; }
    public bool Hidden { get; set; }
    public List<StoredAction> Actions { get; } = [];

    public PermissionObjectType ToModel() => new(Key, Actions.Select(action => action.Key), DisplayName, SourceKey, Hidden);
}
