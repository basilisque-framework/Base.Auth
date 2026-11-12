using Basilisque.Base.Auth.Client.ClientAuth;
using Basilisque.Base.Auth.Client.Credentials;
using Basilisque.Base.Auth.ClientAuth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Client;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Client;

/// <summary>Registers reusable machine clients without imposing a credential storage mechanism.</summary>
public static class ClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds a named private_key_jwt client. The DI container owns credentials returned by the factory.
    /// </summary>
    /// <remarks>Credentials are loaded once per container. Rotation takes effect after restarting that container.
    /// The advanced callback exposes the underlying OpenIddict configuration.</remarks>
    public static IServiceCollection AddBaseAuthClient(
        this IServiceCollection services,
        string registrationName,
        Action<MachineClientOptions> configure,
        Func<IServiceProvider, ClientSigningCredential> credentialFactory,
        Action<OpenIddictClientBuilder>? configureOpenIddict = null)
    {
        if (string.IsNullOrWhiteSpace(registrationName))
            throw new ArgumentException("A registration name is required.", nameof(registrationName));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        if (credentialFactory is null)
            throw new ArgumentNullException(nameof(credentialFactory));

        if (services.Any(descriptor => descriptor.ServiceType == typeof(ClientSigningCredential) &&
            Equals(descriptor.ServiceKey, registrationName)))
            throw new InvalidOperationException("The machine client registration already exists.");

        services.AddOptions<MachineClientOptions>(registrationName).Configure(configure)
            .Validate(options => options.IsValid(), "An HTTPS authority, client ID, valid scopes and renewal margin are required.");

        services.AddKeyedSingleton<ClientSigningCredential>(registrationName, (provider, _) => credentialFactory(provider));

        services.AddOptions<OpenIddictClientOptions>().Configure<IServiceProvider>((options, provider) =>
        {
            var settings = provider.GetRequiredService<IOptionsMonitor<MachineClientOptions>>().Get(registrationName);
            var registration = new OpenIddictClientRegistration
            {
                RegistrationId = registrationName,
                Issuer = settings.Authority,
                ClientId = settings.ClientId
            };
            registration.ClientAuthenticationMethods.Add(ClientAuthenticationMethods.PrivateKeyJwt);
            registration.SigningCredentials.Add(provider.GetRequiredKeyedService<ClientSigningCredential>(registrationName).Credentials);
            foreach (var scope in settings.Scopes)
                registration.Scopes.Add(scope);
            options.Registrations.Add(registration);
        });

        services.AddOpenIddict().AddClient(options =>
        {
            options.AllowClientCredentialsFlow();
            options.DisableTokenStorage();
            options.UseSystemNetHttp();
            configureOpenIddict?.Invoke(options);
        });

        services.TryAddSingleton<ITokenEndpointClient, OpenIddictTokenEndpointClient>();
        services.TryAddSingleton<IAccessTokenProvider, CachedAccessTokenProvider>();

        return services;
    }
}
