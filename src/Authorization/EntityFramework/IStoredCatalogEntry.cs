namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal interface IStoredCatalogEntry : IStoredPermissionKey
{
    string? SourceKey { get; set; }
    bool Hidden { get; set; }
}