using Basilisque.Base.Auth.ClientAuth;
using OpenIddict.Client;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class OpenIddictTokenEndpointClient : ITokenEndpointClient
{
    private readonly OpenIddictClientService _client;

    /// <summary>
    /// Creates a token endpoint adapter using the OpenIddict client service.
    /// </summary>
    public OpenIddictTokenEndpointClient(OpenIddictClientService client)
    {
        _client = client;
    }

    public async Task<AccessToken> RequestAsync(string registrationName, CancellationToken cancellationToken)
    {
        var result = await _client.AuthenticateWithClientCredentialsAsync(new OpenIddictClientModels.ClientCredentialsAuthenticationRequest
        {
            RegistrationId = registrationName,
            CancellationToken = cancellationToken
        }).ConfigureAwait(false);

        return new AccessToken(result.AccessToken, result.AccessTokenExpirationDate);
    }
}
