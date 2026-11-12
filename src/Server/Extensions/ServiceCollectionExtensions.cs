using Basilisque.Base.Auth.Identity;
using Basilisque.Base.Auth.Server.ClientAuth;
using Basilisque.Base.Auth.Server.Configuration;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;

namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>
/// Configures standards-based protocol handling and product identity boundaries.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds an M2M server. Configure persistent stores and production signing/encryption credentials separately.
    /// </summary>
    public static IServiceCollection AddBaseAuthServer(
        this IServiceCollection services,
        Action<AuthServerOptions> configure,
        Action<OpenIddictServerBuilder> configureOpenIddict)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(configureOpenIddict);
        services.AddOptions<AuthServerOptions>().Configure(configure)
            .Validate(options => options.IsValid(), "Valid HTTPS issuer, audience, API scope, endpoint and lifetime are required.")
            .ValidateOnStart();
        services.TryAddScoped<IMachineClientRegistry, MachineClientRegistry>();
        services.TryAddScoped<IServiceActorFactory, RegisteredServiceActorFactory>();
        services.AddOpenIddict().AddServer(options =>
        {
            options.AllowClientCredentialsFlow();
            options.DisableAccessTokenEncryption();
            options.UseAspNetCore().EnableTokenEndpointPassthrough();
            options.AddEventHandler<OpenIddictServerEvents.ProcessAuthenticationContext>(handler => handler
                .UseScopedHandler<ClientAssertionReplayHandler>()
                .SetOrder(int.MaxValue - 100_000));
            configureOpenIddict(options);
        });
        services.AddOptions<OpenIddictServerOptions>().Configure<IOptions<AuthServerOptions>>((protocol, settings) =>
        {
            protocol.Issuer = settings.Value.Issuer;
            protocol.TokenEndpointUris.Add(new Uri(settings.Value.TokenEndpointPath.TrimStart('/'), UriKind.Relative));
            protocol.Scopes.Add(settings.Value.ApiScope);
            protocol.AccessTokenLifetime = settings.Value.AccessTokenLifetime;
        });
        return services;
    }

    /// <summary>Adds access-token validation for an API hosted alongside its Base.Auth authorization server.</summary>
    public static IServiceCollection AddBaseAuthLocalApi(this IServiceCollection services, string audience)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        services.AddOpenIddict().AddValidation(options =>
        {
            options.UseLocalServer();
            options.AddAudiences(audience);
            options.UseAspNetCore();
        });
        return addApiIdentity(services);
    }

    /// <summary>Adds discovery/JWKS-based access-token validation for an API with a remote authorization server.</summary>
    public static IServiceCollection AddBaseAuthRemoteApi(
        this IServiceCollection services, Uri authority, string audience,
        Action<OpenIddictValidationBuilder>? configureOpenIddict = null)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (!authority.IsAbsoluteUri || authority.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("An HTTPS authority is required.", nameof(authority));
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        services.AddOpenIddict().AddValidation(options =>
        {
            options.SetIssuer(authority);
            options.AddAudiences(audience);
            options.UseSystemNetHttp();
            options.UseAspNetCore();
            configureOpenIddict?.Invoke(options);
        });
        return addApiIdentity(services);
    }

    private static IServiceCollection addApiIdentity(IServiceCollection services)
    {
        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        services.AddAuthorization(options => options.AddPolicy(BaseAuthPolicies.Service, policy => policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireClaim(BaseAuthClaims.ActorKind, BaseAuthClaims.Service)
            .RequireAssertion(context =>
            {
                try
                { return context.User.GetBaseAuthActor().Kind == BaseAuthClaims.Service; }
                catch (InvalidOperationException) { return false; }
            })));
        return services;
    }
}
