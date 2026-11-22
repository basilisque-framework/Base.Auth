using OpenIddict.Abstractions;
using OpenIddict.Server;
using Microsoft.Extensions.Localization;
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
