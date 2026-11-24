namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class StoredRole : IStoredPermissionKey
{
    public Guid Id { get; init; }
    public required string Key { get; set; }
    public string? DisplayName { get; set; }
    public List<StoredRule> Rules { get; } = [];

    public PermissionRole ToModel() => new(Key, Rules.Select(rule => rule.ToModel()), DisplayName);
}