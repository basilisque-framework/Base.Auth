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
    /// Gets the actor kind, currently service or, in future flows, user.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the server-assigned installation/customer context.
    /// </summary>
    public string ContextId { get; }

    /// <summary>
    /// Creates an actor in an explicitly assigned application context.
    /// </summary>
    public Actor(string subjectId, string kind, string contextId)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
            throw new ArgumentException("A subject is required.", nameof(subjectId));
        if (string.IsNullOrWhiteSpace(kind))
            throw new ArgumentException("An actor kind is required.", nameof(kind));
        if (string.IsNullOrWhiteSpace(contextId))
            throw new ArgumentException("A context is required.", nameof(contextId));

        SubjectId = subjectId;
        Kind = kind;
        ContextId = contextId;
    }
}
