namespace Basilisque.Base.Auth.Identity;

/// <summary>
/// A product identity independent of the authentication protocol or provider.
/// </summary>
public sealed class Actor
{
    /// <summary>
    /// Gets the stable internal subject identifier.
    /// </summary>
    public string SubjectId { get; }

    /// <summary>
    /// Gets the actor kind, either service or user.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the server-assigned installation or customer context, if assigned.
    /// </summary>
    public string? ContextId { get; }

    /// <summary>
    /// Creates an actor with an optional application context.
    /// </summary>
    public Actor(string subjectId, string kind, string? contextId = null)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
            throw new ArgumentException(Messages.SubjectRequired, nameof(subjectId));
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException(Messages.ActorKindRequired, nameof(kind));
        if (contextId is not null && string.IsNullOrWhiteSpace(contextId))
            throw new ArgumentException(Messages.ContextMustHaveValue, nameof(contextId));

        SubjectId = subjectId;
        Kind = kind;
        ContextId = contextId;
    }
}
