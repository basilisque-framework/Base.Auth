using Basilisque.Base.Auth.Client;
using Basilisque.Base.Auth.Client.Credentials;
using Basilisque.Base.Auth.ClientAuth;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Basilisque.Base.Auth.MachineClient;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length is < 4 or > 5)
        {
            Console.Error.WriteLine(Messages.MachineClientUsage);
            Console.Error.WriteLine(Messages.ClientCertificatePassword);
            return 1;
        }

        try
        {
            var authority = new Uri(args[0], UriKind.Absolute);
            var api = new Uri(args[3], UriKind.Absolute);
            if (authority.Scheme != Uri.UriSchemeHttps || api.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException(Messages.AuthorityAndApiRequireHttps);

            using var certificate = loadCertificate(args[2]);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddBaseAuthClient("sync", options =>
            {
                options.Authority = authority;
                options.ClientId = args[1];
                options.Scopes.Add(args.Length == 5 ? args[4] : "api");
            }, _ => ClientSigningCredential.FromCertificate(certificate));

            using var provider = services.BuildServiceProvider();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var tokens = provider.GetRequiredService<IAccessTokenProvider>();
            var token = await tokens.GetAccessTokenAsync("sync", cancellation.Token).ConfigureAwait(false);
            var cachedToken = await tokens.GetAccessTokenAsync("sync", cancellation.Token).ConfigureAwait(false);
            if (token.Value != cachedToken.Value)
                throw new InvalidOperationException(Messages.ExpectedCachedToken);

            // Business HTTP remains in the application. No API transport is hidden in Base.Auth.
            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
            using var http = new HttpClient(handler);
            using var request = new HttpRequestMessage(HttpMethod.Get, api);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
            using var response = await http.SendAsync(request, cancellation.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var actor = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            var identity = actor.RootElement;
            if (identity.GetProperty("kind").GetString() != "service")
                throw new InvalidOperationException(Messages.ExpectedServiceActor);
            Console.WriteLine(Messages.ServiceAuthenticated,
                identity.GetProperty("subjectId").GetString(), identity.GetProperty("contextId").GetString());
            return 0;
        }
        catch (Exception exception)
        {
            // Protocol exception text can contain remote response data. Do not log it or bearer tokens.
            Console.Error.WriteLine(Messages.MachineClientFailed, exception.GetType().Name);
            return 1;
        }
    }

    private static X509Certificate2 loadCertificate(string path)
    {
        var password = Environment.GetEnvironmentVariable("BASE_AUTH_CLIENT_CERTIFICATE_PASSWORD");
#if NET48
        // .NET Framework loads PFX files through the certificate constructor.
        return new X509Certificate2(path, password, X509KeyStorageFlags.EphemeralKeySet);
#else
        return X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.EphemeralKeySet);
#endif
    }
}
