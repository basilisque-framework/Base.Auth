namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>
/// Admission policies for product users and services.
/// </summary>
public static class BaseAuthPolicies
{
    /// <summary>
    /// Requires an access token representing a product user.
    /// </summary>
    public const string User = "Base.Auth.User";

    /// <summary>
    /// Requires a validated service token with an internal identity and context.
    /// </summary>
    public const string Service = "Base.Auth.Service";
}
