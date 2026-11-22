namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal interface IStoredPermissionKey
{
    Guid Id { get; }
    string Key { get; set; }
    string? DisplayName { get; set; }
}
