using Basilisque.Base.Auth.Client;
using Basilisque.Base.Auth.Client.Credentials;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;
using Basilisque.Base.Auth.Server.ClientAuth;
using Basilisque.Base.Auth.Server.Extensions;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Server.Integration.Tests;

internal sealed class ProtocolTestEnvironment : IAsyncDisposable
{
    internal const string ClientId = "demo-client";
    internal const string ContextId = "installation-one";
    internal const string Audience = "product-api";
    internal static readonly Uri Issuer = new("https://auth.example/");
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "base-auth-tests-" + Guid.NewGuid().ToString("N"));
    private readonly RSA _serverKey = RSA.Create(2048);
    private WebApplication _application = null!;
    internal readonly RSA ClientKey = RSA.Create(2048);
    internal HttpClient Http = null!;

    internal static async Task<ProtocolTestEnvironment> CreateAsync(bool localUsers = false, string? machineContextId = ContextId)
    {
        var environment = new ProtocolTestEnvironment();
        try
        { await environment.initializeAsync(localUsers, machineContextId); return environment; }
        catch { await environment.DisposeAsync(); throw; }
    }

    private async Task initializeAsync(bool localUsers, string? machineContextId)
    {
        Directory.CreateDirectory(_directory);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SQLite",
            ["Database:ConnectionStrings:SQLite:ConnectionString"] = $"Data Source={Path.Combine(_directory, "auth.db")};Pooling=False"
        });
        Basilisque.Base.Auth.DataAccess.EntityFramework.SQLite.IServiceCollectionExtensions.RegisterServices(builder.Services);
        builder.Services.AddBaseAuthRelationalPersistence();
        builder.Services.AddBaseAuthServer(options => { options.Issuer = Issuer; options.Audience = Audience; },
            options => options.AddSigningKey(new RsaSecurityKey(_serverKey) { KeyId = "server-key" }).AddEphemeralEncryptionKey());
        builder.Services.AddBaseAuthLocalApi(Audience);
        if (localUsers)
        {
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddBaseAuthLocalUsers().AddBaseAuthRelationalUserStore();
        }
        _application = builder.Build();
        await using (var scope = _application.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
            if (localUsers)
                await scope.ServiceProvider.GetRequiredService<LocalIdentityDbContext>().Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<IMachineClientRegistry>().RegisterAsync(ClientId, machineContextId, PublicKeys(ClientKey));
        }
        _application.UseAuthentication();
        _application.UseAuthorization();
        _application.MapBaseAuthTokenEndpoint();
        _application.MapGet("/api/ping", (HttpContext context) => Results.Ok(context.User.GetBaseAuthActor()))
            .RequireAuthorization(BaseAuthPolicies.Service);
        if (localUsers)
        {
            _application.MapBaseAuthLocalUserAuthorization();
            _application.MapGet("/api/me", (HttpContext context) => Results.Ok(context.User.GetBaseAuthActor()))
                .RequireAuthorization(BaseAuthPolicies.User);
            _application.MapPost("/test/sign-in", async (HttpContext context, SignInManager<LocalUser> signIn) =>
            {
                var password = context.Request.Query.ContainsKey("wrong-password") ? "invalid" : LocalUserTests.Password;
                var result = await signIn.PasswordSignInAsync(LocalUserTests.Email, password, false, false);
                return result.Succeeded ? Results.NoContent() : Results.Unauthorized();
            });
        }
        await _application.StartAsync();
        Http = _application.GetTestClient();
        Http.BaseAddress = Issuer;
    }

    internal AsyncServiceScope CreateScope() => _application.Services.CreateAsyncScope();

    internal ServiceProvider CreateClient(RSA? key = null, bool cacheAccessTokens = true)
        => CreateClient(ClientId, () => ClientSigningCredential.FromRsa(key ?? ClientKey, "client-key"), cacheAccessTokens);

    internal ServiceProvider CreateRuntimeClients(Func<HttpRequestMessage, CancellationToken, Task>? beforeSend = null)
        => createClientServices(beforeSend).AddBaseAuthRuntimeClients().BuildServiceProvider();

    internal ServiceProvider CreateClient(string clientId, Func<ClientSigningCredential> createCredential, bool cacheAccessTokens = true)
    {
        var services = createClientServices();
        services.AddBaseAuthClient("test", options =>
        {
            options.Authority = Issuer;
            options.ClientId = clientId;
            options.CacheAccessTokens = cacheAccessTokens;
            options.Scopes.Add("api");
        }, _ => createCredential());
        return services.BuildServiceProvider();
    }

    private ServiceCollection createClientServices(Func<HttpRequestMessage, CancellationToken, Task>? beforeSend = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.PostConfigureAll<HttpClientFactoryOptions>(options => options.HttpMessageHandlerBuilderActions.Add(
            builder => builder.AdditionalHandlers.Add(new TestServerTransport(_application.GetTestServer().CreateHandler(), beforeSend))));
        return services;
    }

    private sealed class TestServerTransport : DelegatingHandler
    {
        private readonly HttpMessageInvoker _transport;
        private readonly Func<HttpRequestMessage, CancellationToken, Task>? _beforeSend;

        internal TestServerTransport(HttpMessageHandler handler, Func<HttpRequestMessage, CancellationToken, Task>? beforeSend)
        {
            _transport = new HttpMessageInvoker(handler);
            _beforeSend = beforeSend;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_beforeSend is not null)
                await _beforeSend(request, cancellationToken);

            return await _transport.SendAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _transport.Dispose();
            base.Dispose(disposing);
        }
    }

    internal static JsonWebKeySet PublicKeys(RSA key)
    {
        var jwks = new JsonWebKeySet();
        using var credential = ClientSigningCredential.FromRsa(key, "client-key");
        jwks.Keys.Add(credential.ExportPublicJwk());
        return jwks;
    }

    internal string CreateAssertion(string? audience = null, DateTime? expires = null, RSA? key = null, string type = "client-authentication+jwt")
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = ClientId,
            Audience = audience ?? Issuer.AbsoluteUri,
            Subject = new ClaimsIdentity([new Claim(Claims.Subject, ClientId), new Claim(Claims.JwtId, Guid.NewGuid().ToString("N"))]),
            IssuedAt = now.AddSeconds(-2),
            NotBefore = now.AddSeconds(-2),
            Expires = expires ?? now.AddMinutes(2),
            TokenType = type,
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(key ?? ClientKey) { KeyId = "client-key" }, SecurityAlgorithms.RsaSha256)
        });
    }

    internal Task<HttpResponseMessage> ExchangeAsync(string assertion) => Http.PostAsync("connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = ClientId,
        ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
        ["client_assertion"] = assertion,
        ["scope"] = "api"
    }));

    internal async Task DisableAsync()
    {
        await using var scope = _application.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMachineClientRegistry>().DisableAsync(ClientId);
    }

    internal async Task RotateKeyAsync(RSA key)
    {
        await using var scope = _application.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMachineClientRegistry>().ReplacePublicKeysAsync(ClientId, PublicKeys(key));
    }

    internal async Task RegisterAsync(string clientId, JsonWebKeySet keys)
    {
        await using var scope = _application.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMachineClientRegistry>().RegisterAsync(clientId, ContextId, keys);
    }

    internal async Task<string> GetTokenAsync()
    {
        using var response = await ExchangeAsync(CreateAssertion());
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(body);
        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("access_token").GetString()!;
    }

    internal string CreateApiToken(string issuer = "https://auth.example/", string audience = Audience,
        string tokenType = "at+jwt", DateTime? expires = null, RSA? signingKey = null)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            TokenType = tokenType,
            IssuedAt = now.AddMinutes(-2),
            NotBefore = now.AddMinutes(-2),
            Expires = expires ?? now.AddMinutes(2),
            Subject = new ClaimsIdentity([
                new Claim(Claims.Subject, "internal-service"),
                new Claim("actor_type", "service"),
                new Claim("context_id", ContextId)
            ]),
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(signingKey ?? _serverKey) { KeyId = "server-key" }, SecurityAlgorithms.RsaSha256)
        });
    }

    public async ValueTask DisposeAsync()
    {
        Http?.Dispose();
        if (_application is not null)
            await _application.DisposeAsync();
        ClientKey.Dispose();
        _serverKey.Dispose();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
