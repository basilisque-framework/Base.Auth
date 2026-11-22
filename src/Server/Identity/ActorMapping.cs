using Basilisque.Base.Auth.Identity;
using System.Security.Claims;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// Maps validated product tokens to the common application identity model.
/// </summary>
public static class ActorMapping
{
    /// <param name="principal">The authenticated principal from which to read the actor.</param>
    extension(ClaimsPrincipal principal)
    {
        /// <summary>
        /// Reads the product identity from an authenticated principal with unambiguous claims.
        /// </summary>
        public Actor GetBaseAuthActor()
        {
            ArgumentNullException.ThrowIfNull(principal);
            if (principal.Identity?.IsAuthenticated != true)
                throw new InvalidOperationException(Messages.PrincipalNotAuthenticated);

            return new Actor(single("sub"), single(BaseAuthClaims.ActorKind), optional(BaseAuthClaims.ContextId));

            string single(string type) => optional(type)
                ?? throw new InvalidOperationException(Messages.AmbiguousProductIdentity);

            string? optional(string type)
            {
                var claims = principal.FindAll(type).ToArray();

                if (claims.Length == 0)
                    return null;

                if (claims.Length != 1 || string.IsNullOrWhiteSpace(claims[0].Value))
                    throw new InvalidOperationException(Messages.AmbiguousProductIdentity);

                return claims[0].Value;
            }
        }
    }
}
