using Basilisque.Base.Auth.ClientAuth;
using System.Threading;
using OpenIddict.Client;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal interface ITokenEndpointClient
{
    Task<AccessToken> RequestAsync(string registrationName, CancellationToken cancellationToken,
        OpenIddictClientRegistration? registration = null);
}
