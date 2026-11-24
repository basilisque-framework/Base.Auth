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