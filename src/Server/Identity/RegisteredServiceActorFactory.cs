using Basilisque.Base.Auth.Identity;
using Basilisque.Base.Auth.Server.ClientAuth;
using OpenIddict.Abstractions;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Server.Identity;

internal sealed class RegisteredServiceActorFactory : IServiceActorFactory
{
    private readonly IOpenIddictApplicationManager _applications;

    /// <summary>
    /// Creates an actor factory backed by registered OpenIddict applications.
    /// </summary>
    public RegisteredServiceActorFactory(IOpenIddictApplicationManager applications)
    {
        _applications = applications;
    }

    public async ValueTask<ClaimsPrincipal> CreateAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var application = await _applications.FindByClientIdAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException(Messages.AuthenticatedClientRegistrationNotFound);
        var properties = await _applications.GetPropertiesAsync(application, cancellationToken);
        var subject = properties.TryGetValue(MachineClientRegistry.SubjectProperty, out var subjectValue) ? subjectValue.GetString() : null;
        var context = properties.TryGetValue(MachineClientRegistry.ContextProperty, out var contextValue) ? contextValue.GetString() : null;
        if (string.IsNullOrWhiteSpace(subject) || (context is not null && string.IsNullOrWhiteSpace(context)))
            throw new InvalidOperationException(Messages.InvalidProductIdentityMetadata);

        var identity = new ClaimsIdentity("Base.Auth", Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, subject);
        identity.SetClaim(Claims.ClientId, clientId);
        identity.SetClaim(BaseAuthClaims.ActorKind, BaseAuthClaims.Service);
        identity.SetClaim(BaseAuthClaims.ContextId, context);
        identity.SetDestinations(_ => [Destinations.AccessToken]);

        return new ClaimsPrincipal(identity);
    }
}
