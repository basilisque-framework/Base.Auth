namespace Basilisque.Base.Auth.Identity;

/// <summary>
/// Defines the claim contract at the product authentication boundary.
/// </summary>
public static class BaseAuthClaims
{
    /// <summary>
    /// The actor kind claim.
    /// </summary>
    public const string ActorKind = "actor_type";

    /// <summary>
    /// The application context claim.
    /// </summary>
    public const string ContextId = "context_id";

    /// <summary>
    /// The service actor kind.
    /// </summary>
    public const string Service = "service";
}
