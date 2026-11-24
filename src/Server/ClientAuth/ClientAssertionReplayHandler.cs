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

using Microsoft.Extensions.Localization;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Server.ClientAuth;

internal sealed class ClientAssertionReplayHandler : IOpenIddictServerHandler<OpenIddictServerEvents.ProcessAuthenticationContext>
{
    private readonly IClientAssertionReplayStore _replayStore;
    private readonly IStringLocalizer<AuthProtocolMessages> _localizer;

    /// <summary>
    /// Creates an assertion handler using the persistent replay store.
    /// </summary>
    public ClientAssertionReplayHandler(IClientAssertionReplayStore replayStore, IStringLocalizer<AuthProtocolMessages> localizer)
    {
        _replayStore = replayStore;
        _localizer = localizer;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.ProcessAuthenticationContext context)
    {
        var principal = context.ClientAssertionPrincipal;
        if (principal is null)
            return;

        var identifier = principal.GetClaim(Claims.JwtId);
        var expiresAt = principal.GetExpirationDate();
        var issuedAt = principal.GetCreationDate();
        var now = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > 200 || !expiresAt.HasValue ||
            !issuedAt.HasValue || expiresAt <= now || issuedAt > now.AddSeconds(30) ||
            expiresAt - issuedAt > TimeSpan.FromMinutes(5) ||
            !await _replayStore.TryUseAsync(context.ClientId!, identifier, expiresAt.Value.AddSeconds(30), context.CancellationToken))
        {
            context.Reject(Errors.InvalidClient, AuthProtocolMessageLocalizer.Resolve(_localizer, AuthProtocolMessages.ClientAssertionInvalidOrReplayed));
        }
    }
}