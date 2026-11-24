namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class StoredRule
{
    public Guid RoleId { get; init; }
    public Guid ObjectId { get; init; }
    public Guid ActionId { get; init; }
    public required PermissionEffect Effect { get; set; }
    public StoredObject Object { get; init; } = null!;
    public StoredAction Action { get; init; } = null!;

    public RolePermissionRule ToModel() => new(Object.Key, Action.Key, Effect);
}