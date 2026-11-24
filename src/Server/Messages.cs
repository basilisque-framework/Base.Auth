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

using System.Globalization;
using System.Resources;

namespace Basilisque.Base.Auth.Server;

internal static class Messages
{
    private static readonly ResourceManager _resources = new(typeof(Messages));

    internal static string InvalidLocalUserContext => get(nameof(InvalidLocalUserContext));

    internal static string AmbiguousProductIdentity => get(nameof(AmbiguousProductIdentity));

    internal static string AuthenticatedClientNotFound => get(nameof(AuthenticatedClientNotFound));

    internal static string AuthenticatedClientRegistrationNotFound => get(nameof(AuthenticatedClientRegistrationNotFound));

    internal static string ClientAlreadyExists => get(nameof(ClientAlreadyExists));

    internal static string ClientNotFound => get(nameof(ClientNotFound));

    internal static string HttpsAuthorityRequired => get(nameof(HttpsAuthorityRequired));

    internal static string InvalidAuthServerOptions => get(nameof(InvalidAuthServerOptions));

    internal static string InvalidProductIdentityMetadata => get(nameof(InvalidProductIdentityMetadata));

    internal static string InvalidPublicKeyCount => get(nameof(InvalidPublicKeyCount));

    internal static string InvalidPublicKeys => get(nameof(InvalidPublicKeys));

    internal static string PrincipalNotAuthenticated => get(nameof(PrincipalNotAuthenticated));

    internal static string RequestNotProcessedByMiddleware => get(nameof(RequestNotProcessedByMiddleware));

    internal static string RequestNotProcessedByServerMiddleware => get(nameof(RequestNotProcessedByServerMiddleware));

    internal static string RsaPublicKeyTooShort => get(nameof(RsaPublicKeyTooShort));

    internal static string UnsupportedEcPublicKey => get(nameof(UnsupportedEcPublicKey));

    private static string get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)!;

    internal static string? GetProtocolMessage(string key, bool includeParents)
        => includeParents ? get(key) : new ResourceManager(typeof(Messages))
            .GetResourceSet(CultureInfo.CurrentUICulture, createIfNotExists: true, tryParents: false)?.GetString(key);
}