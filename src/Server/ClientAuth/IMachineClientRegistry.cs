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

using Microsoft.IdentityModel.Tokens;

namespace Basilisque.Base.Auth.Server.ClientAuth;

/// <summary>
/// Administrative provisioning operations; never expose these as anonymous enrollment endpoints.
/// </summary>
public interface IMachineClientRegistry
{
    /// <summary>
    /// Creates a client with a server-assigned subject and an optional context. Pass null for no context.
    /// Existing registrations are never overwritten.
    /// </summary>
    ValueTask RegisterAsync(string clientId, string? contextId, JsonWebKeySet publicKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces registered public keys, allowing an overlap during rotation. Keeps subject and context stable.
    /// </summary>
    ValueTask ReplacePublicKeysAsync(string clientId, JsonWebKeySet publicKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prevents future token acquisition. Previously issued JWTs remain valid until expiration.
    /// </summary>
    ValueTask DisableAsync(string clientId, CancellationToken cancellationToken = default);
}