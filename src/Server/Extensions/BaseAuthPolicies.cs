namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>
/// Admission policies for the initial machine-only profile.
/// </summary>
public static class BaseAuthPolicies
{
    /// <summary>
    /// Requires a validated service token with an internal identity and context.
    /// </summary>
    public const string Service = "Base.Auth.Service";
}
