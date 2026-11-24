using Basilisque.Base.Auth.Abstractions.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// Resolves an authenticated local account to its admitted product identity.
/// </summary>
public interface ILocalUserAdmission
{
    /// <summary>
    /// Returns the admitted actor, or null when the account has no product access.
    /// </summary>
    ValueTask<Actor?> ResolveAsync(LocalUser user, CancellationToken cancellationToken = default);
}