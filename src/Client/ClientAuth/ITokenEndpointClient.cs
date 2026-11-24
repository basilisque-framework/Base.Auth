using Basilisque.Base.Auth.Abstractions.ClientAuth;
using OpenIddict.Client;
using System.Threading;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal interface ITokenEndpointClient
{
    Task<AccessToken> RequestAsync(string registrationName, CancellationToken cancellationToken,
        OpenIddictClientRegistration? registration = null);
}