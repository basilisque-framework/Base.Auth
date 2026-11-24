namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class StoredAction : IStoredCatalogEntry
{
    public Guid Id { get; init; }
    public required string Key { get; set; }
    public string? DisplayName { get; set; }
    public string? SourceKey { get; set; }
    public bool Hidden { get; set; }
}