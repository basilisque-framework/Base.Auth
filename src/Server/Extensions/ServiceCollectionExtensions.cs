using Basilisque.Base.Auth.Identity;
using Basilisque.Base.Auth.Server.ClientAuth;
using Basilisque.Base.Auth.Server.Configuration;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;

namespace Basilisque.Base.Auth.Server.Extensions;

/// <summary>
/// Configures standards-based protocol handling and product identity boundaries.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds an M2M server. Configure persistent stores and production signing/encryption credentials separately.
        /// </summary>
        public IServiceCollection AddBaseAuthServer(
            Action<AuthServerOptions> configure,
            Action<OpenIddictServerBuilder> configureOpenIddict)
        {
            ArgumentNullException.ThrowIfNull(configure);
            ArgumentNullException.ThrowIfNull(configureOpenIddict);
            services.AddOptions<AuthServerOptions>().Configure(configure)
                .ValidateOnStart();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AuthServerOptions>, AuthServerOptionsValidator>());
            services.TryAddScoped<IMachineClientRegistry, MachineClientRegistry>();
            services.TryAddScoped<IServiceActorFactory, RegisteredServiceActorFactory>();
            services.TryAddSingleton<IStringLocalizer<AuthProtocolMessages>, AuthProtocolMessageLocalizer>();
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

        /// <summary>
        /// Adds access-token validation for an API hosted alongside its Base.Auth authorization server.
        /// </summary>
        public IServiceCollection AddBaseAuthLocalApi(string audience)
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

        /// <summary>
        /// Adds discovery/JWKS-based access-token validation for an API with a remote authorization server.
        /// </summary>
        public IServiceCollection AddBaseAuthRemoteApi(
            Uri authority, string audience,
            Action<OpenIddictValidationBuilder>? configureOpenIddict = null)
        {
            ArgumentNullException.ThrowIfNull(authority);
            if (!authority.IsAbsoluteUri || authority.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException(Messages.HttpsAuthorityRequired, nameof(authority));
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
    }

    private static IServiceCollection addApiIdentity(IServiceCollection services)
    {
        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        services.AddAuthorization(options =>
        {
            addActorPolicy(options, BaseAuthPolicies.Service, BaseAuthClaims.Service);
            addActorPolicy(options, BaseAuthPolicies.User, BaseAuthClaims.User);
        });
        return services;
    }

    private static void addActorPolicy(AuthorizationOptions options, string policyName, string actorKind)
        => options.AddPolicy(policyName, policy => policy
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireClaim(BaseAuthClaims.ActorKind, actorKind)
            .RequireAssertion(context =>
            {
                try
                { return context.User.GetBaseAuthActor().Kind == actorKind; }
                catch (InvalidOperationException) { return false; }
            }));
}
