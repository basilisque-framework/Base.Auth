using Basilisque.Base.Auth.Identity;
using System.Security.Claims;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// Maps validated product tokens to the common application identity model.
/// </summary>
public static class ActorMapping
{
    /// <summary>
    /// Reads the product identity from an authenticated principal with unambiguous claims.
    /// </summary>
    public static Actor GetBaseAuthActor(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("The principal is not authenticated.");
        return new Actor(single("sub"), single(BaseAuthClaims.ActorKind), single(BaseAuthClaims.ContextId));

        string single(string type)
        {
            var claims = principal.FindAll(type).ToArray();
            if (claims.Length != 1 || string.IsNullOrWhiteSpace(claims[0].Value))
                throw new InvalidOperationException("The principal does not contain an unambiguous product identity.");
            return claims[0].Value;
        }
    }
}
