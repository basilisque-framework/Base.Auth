using Basilisque.Base.Auth.Abstractions.ClientAuth;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Client;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Abstractions.OpenIddictExceptions;
using static OpenIddict.Client.OpenIddictClientEvents;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class OpenIddictTokenEndpointClient : ITokenEndpointClient
{
    private readonly OpenIddictClientService _client;
    private readonly IServiceScopeFactory _scopes;

    /// <summary>
    /// Creates a token endpoint adapter using the OpenIddict client service.
    /// </summary>
    public OpenIddictTokenEndpointClient(OpenIddictClientService client, IServiceScopeFactory scopes)
    {
        _client = client;
        _scopes = scopes;
    }

    public async Task<AccessToken> RequestAsync(string registrationName, CancellationToken cancellationToken,
        OpenIddictClientRegistration? registration = null)
    {
        if (registration is not null)
            return await requestRuntimeAsync(registration, cancellationToken).ConfigureAwait(false);

        var result = await _client.AuthenticateWithClientCredentialsAsync(new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
        {
            RegistrationId = registrationName,
            CancellationToken = cancellationToken
        }).ConfigureAwait(false);

        return new AccessToken(result.AccessToken, result.AccessTokenExpirationDate);
    }

    private async Task<AccessToken> requestRuntimeAsync(OpenIddictClientRegistration registration, CancellationToken cancellationToken)
    {
        // Pin the registration to this transaction even if the connection changes during the request.
        await using var scope = _scopes.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IOpenIddictClientFactory>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IOpenIddictClientDispatcher>();
        var context = new ProcessAuthenticationContext(await factory.CreateTransactionAsync().ConfigureAwait(false))
        {
            CancellationToken = cancellationToken,
            GrantType = GrantTypes.ClientCredentials,
            RegistrationId = registration.RegistrationId,
            Registration = registration,
            Request = new(),
            TokenRequest = new()
        };
        await dispatcher.DispatchAsync(context).ConfigureAwait(false);
        if (context.IsRejected)
            throw new ProtocolException(Messages.TokenRequestFailed, context.Error, context.ErrorDescription, context.ErrorUri);

        return new AccessToken(context.BackchannelAccessToken!, context.BackchannelAccessTokenExpirationDate);
    }
}