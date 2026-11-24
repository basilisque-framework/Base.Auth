/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Basilisque.Base.Auth.Abstractions.Identity;
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