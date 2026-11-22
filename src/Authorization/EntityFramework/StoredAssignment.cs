namespace Basilisque.Base.Auth.Authorization.EntityFramework;

internal sealed class StoredAssignment
{
    public Guid Id { get; init; }
    public Guid RoleId { get; init; }
    public StoredRole Role { get; init; } = null!;
    public required string ActorKind { get; init; }
    public required string SubjectId { get; init; }
    public required string ScopeId { get; init; }
    public DateTime? ValidFromUtc { get; init; }
    public DateTime? ValidUntilUtc { get; init; }

    public RoleAssignment ToModel() => new(Role.Key, ActorKind, SubjectId, ScopeId,
        ValidFromUtc is DateTime from ? new DateTimeOffset(DateTime.SpecifyKind(from, DateTimeKind.Utc)) : null,
        ValidUntilUtc is DateTime until ? new DateTimeOffset(DateTime.SpecifyKind(until, DateTimeKind.Utc)) : null);
}
