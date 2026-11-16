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
    /// <param name="services">The service collection that receives the machine-client registrations.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a named machine client that authenticates using a signed client assertion.
        /// </summary>
        /// <param name="registrationName">
        /// A unique local name for this registration.
        /// Pass the same name to <see cref="IAccessTokenProvider.GetAccessTokenAsync"/> to select the client.
        /// </param>
        /// <param name="configure">Configures the HTTPS authority, the client ID registered at that authority, the requested scopes, and token cache settings.</param>
        /// <param name="credentialFactory">
        /// Resolves the signing credential for the registration from the service provider.
        /// The credential must be able to sign with the private key corresponding to the public key registered at the authority.
        /// For example, the factory can load a certificate or open a protected key package.
        /// </param>
        /// <param name="configureOpenIddict">
        /// An optional callback for advanced configuration of the shared OpenIddict client.
        /// Changes to shared client settings may affect other registrations.
        /// </param>
        /// <returns>The service collection for further configuration.</returns>
        /// <remarks>
        /// The container loads the credential once per registration and disposes the returned wrapper.
        /// Restart the host or recreate its container to load a rotated credential.
        /// </remarks>
        public IServiceCollection AddBaseAuthClient(
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
            services.AddSingleton(new FixedMachineClientName(registrationName));

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

            addClientServices(services, configureOpenIddict);
            return services;
        }

        /// <summary>Enables outbound machine connections that can be managed while the host is running.</summary>
        /// <param name="configureOpenIddict">An optional callback for the shared OpenIddict client settings.</param>
        /// <returns>The service collection for further configuration.</returns>
        public IServiceCollection AddBaseAuthRuntimeClients(
            Action<OpenIddictClientBuilder>? configureOpenIddict = null)
        {
            services.TryAddSingleton<RuntimeMachineClientRegistry>();
            services.TryAddSingleton<IRuntimeMachineClientRegistry>(provider => provider.GetRequiredService<RuntimeMachineClientRegistry>());
            addClientServices(services, configureOpenIddict);
            return services;
        }
    }

    private static void addClientServices(IServiceCollection services, Action<OpenIddictClientBuilder>? configureOpenIddict)
    {
        services.AddOpenIddict().AddClient(options =>
        {
            options.AllowClientCredentialsFlow();
            options.DisableTokenStorage();
            options.UseSystemNetHttp();
            configureOpenIddict?.Invoke(options);
        });

        services.TryAddSingleton<ITokenEndpointClient, OpenIddictTokenEndpointClient>();
        services.TryAddSingleton<IAccessTokenProvider, CachedAccessTokenProvider>();
    }
}
