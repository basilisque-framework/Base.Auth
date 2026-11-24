using System.Security.Claims;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// Maps an already authenticated protocol client to a product service identity.
/// </summary>
public interface IServiceActorFactory
{
    /// <summary>
    /// Creates a service principal using trusted server-side registration metadata.
    /// </summary>
    ValueTask<ClaimsPrincipal> CreateAsync(string clientId, CancellationToken cancellationToken = default);
}