using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using TUnit.Core.Enums;

namespace Basilisque.Base.Auth.Client.Integration.Tests;

/// <summary>Runs .NET Framework and modern .NET processes against persisted keys and an HTTPS service.</summary>
[Category("Integration")]
[Category("WindowsCompatibility")]
public sealed class CrossFrameworkTests
{
    /// <summary>Recovers a private key produced by the other framework and verifies a real signature.</summary>
    [Test, RunOn(OS.Windows)]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Load_ShouldVerifySignature_WithPackageFromOtherFramework(bool produceWithNetFramework)
    {
        // Arrange
        await using var environment = new CompatibilityEnvironment();
        var producer = produceWithNetFramework ? "net48" : CompatibilityEnvironment.CurrentFramework;
        var consumer = produceWithNetFramework ? CompatibilityEnvironment.CurrentFramework : "net48";
        var keyRing = Path.Combine(environment.DirectoryPath, "key-ring");
        Directory.CreateDirectory(keyRing);
        var package = Path.Combine(environment.DirectoryPath, "key-package");
        var publicKey = Path.Combine(environment.DirectoryPath, "public.xml");
        var provisioned = await environment.StartProgram("Client.Credentials.DataProtection.Compatibility", producer,
            ["protect", keyRing, package, publicKey]).CompleteAsync();
        provisioned.EnsureSuccess();
        // Act
        var result = await environment.StartProgram("Client.Credentials.DataProtection.Compatibility", consumer,
            ["verify", keyRing, package, publicKey]).CompleteAsync();
        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0).Because($"The verification process must succeed. {result.Output} {result.Error}");
    }

    /// <summary>Exercises discovery, private_key_jwt, token caching and the protected API over real HTTPS.</summary>
    [Test, RunOn(OS.Windows)]
    [Arguments(true)]
    [Arguments(false)]
    public async Task GetAccessTokenAsync_ShouldCallProtectedApi_InFrameworkProcess(bool useNetFrameworkClient)
    {
        // Arrange
        await using var environment = new CompatibilityEnvironment();
        var serverFramework = CompatibilityEnvironment.CurrentFramework;
        var clientFramework = useNetFrameworkClient ? "net48" : serverFramework;
        var check = await environment.RunDotnetAsync("dev-certs", "https", "--check", "--trust");
        if (check.ExitCode != 0)
            throw new InvalidOperationException("A trusted HTTPS development certificate is required. The tests do not install certificates or bypass TLS validation.");
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Compatibility Client", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var certificatePath = Path.Combine(environment.DirectoryPath, "client.pfx");
        await File.WriteAllBytesAsync(certificatePath, certificate.Export(X509ContentType.Pfx, password));
        var parameters = rsa.ExportParameters(false);
        var publicKeys = Path.Combine(environment.DirectoryPath, "keys.public.json");
        await File.WriteAllTextAsync(publicKeys, JsonSerializer.Serialize(new
        {
            keys = new[] { new { kty = "RSA", kid = certificate.Thumbprint, use = "sig", alg = "RS256",
                n = base64Url(parameters.Modulus!), e = base64Url(parameters.Exponent!) } }
        }));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var authority = $"https://localhost:{port}/";
        var variables = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["BASE_AUTH_CLIENT_CERTIFICATE_PASSWORD"] = password
        };
        var serverArguments = new[] { "--contentRoot", Path.Combine(environment.Root, "samples", "MachineService"),
            "--Auth:Issuer", authority, "--Database:Provider", "SQLite", "--Database:ConnectionStrings:SQLite:ConnectionString",
            "Data Source=" + Path.Combine(environment.DirectoryPath, "auth.db") + ";Pooling=False" };
        var registered = await environment.StartProgram("MachineService", serverFramework, serverArguments.Concat(
            ["--initialize-database", "true", "--register-client", "compatibility-client", "--context", "compatibility-installation", "--public-jwks", publicKeys]), variables).CompleteAsync();
        registered.EnsureSuccess();
        var server = environment.StartProgram("MachineService", serverFramework, serverArguments.Concat(["--urls", authority]), variables);
        await waitForServerAsync(server, authority);
        // Act
        var result = await environment.StartProgram("MachineClient", clientFramework,
            [authority, "compatibility-client", certificatePath, authority + "api/ping"], variables).CompleteAsync();
        // Assert
        await Assert.That(result.ExitCode).IsEqualTo(0).Because($"The client process must succeed. {result.Output} {result.Error}");
        await Assert.That(result.Output.Contains("compatibility-installation", StringComparison.Ordinal)).IsTrue();
    }

    private static string base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Exercises the reference host's actual Identity login form and authorization code exchange.</summary>
    [Test, RunOn(OS.Windows)]
    public async Task Login_ShouldAuthorizeUserThroughPkce_InReferenceHost()
    {
        // Arrange
        await using var environment = new CompatibilityEnvironment();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var authority = $"https://localhost:{port}/";
        const string Email = "reader@example.test";
        const string ContextId = "local-browser-test";
        const string ClientId = "local-browser-client";
        const string Redirect = "https://client.example/callback";
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) + "!a";
        var serverArguments = new[] { "--contentRoot", Path.Combine(environment.Root, "samples", "MachineService"),
            "--Auth:Issuer", authority, "--Database:Provider", "SQLite", "--Database:ConnectionStrings:SQLite:ConnectionString",
            "Data Source=" + Path.Combine(environment.DirectoryPath, "auth.db") + ";Pooling=False" };
        var framework = CompatibilityEnvironment.CurrentFramework;
        var variables = new Dictionary<string, string> { ["ASPNETCORE_ENVIRONMENT"] = "Development" };
        (await environment.StartProgram("MachineService", framework, serverArguments.Concat(
            ["--initialize-database", "true", "--create-user", Email, "--context", ContextId]),
            new Dictionary<string, string>(variables) { ["BASE_AUTH_LOCAL_USER_PASSWORD"] = password }).CompleteAsync()).EnsureSuccess();
        (await environment.StartProgram("MachineService", framework, serverArguments.Concat(
            ["--register-user-client", ClientId, "--redirect-uri", Redirect]), variables).CompleteAsync()).EnsureSuccess();
        var server = environment.StartProgram("MachineService", framework, serverArguments.Concat(["--urls", authority]), variables);
        await waitForServerAsync(server, authority);
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
        using var browser = new HttpClient(handler) { BaseAddress = new Uri(authority), Timeout = TimeSpan.FromSeconds(10) };
        var verifier = base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var authorize = $"connect/authorize?client_id={ClientId}&redirect_uri={Uri.EscapeDataString(Redirect)}" +
            $"&response_type=code&scope=openid%20api&code_challenge={challenge}&code_challenge_method=S256&state=test-state&nonce=test-nonce";
        using var loginRedirect = await browser.GetAsync(authorize);
        var loginLocation = loginRedirect.Headers.Location ?? throw new InvalidOperationException("Missing login redirect.");
        using var loginForm = await browser.GetAsync(loginLocation);
        var html = await loginForm.Content.ReadAsStringAsync();
        if (!loginForm.IsSuccessStatusCode)
            throw new InvalidOperationException($"The login form returned {loginForm.StatusCode}. {html}");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        if (token.Length == 0)
            throw new InvalidOperationException("The Identity login form did not render an antiforgery token.");
        using var login = await browser.PostAsync(loginLocation, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = Email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = token
        }));
        var authorizationLocation = login.Headers.Location ?? throw new InvalidOperationException("Login did not return to authorization.");
        using var authorization = await browser.GetAsync(authorizationLocation);
        var callback = authorization.Headers.Location ?? throw new InvalidOperationException("Authorization did not redirect to the client.");
        var code = callback.Query.TrimStart('?').Split('&').Single(value => value.StartsWith("code=", StringComparison.Ordinal))[5..];
        using var exchange = await browser.PostAsync("connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["redirect_uri"] = Redirect,
            ["code"] = Uri.UnescapeDataString(code),
            ["code_verifier"] = verifier
        }));
        exchange.EnsureSuccessStatusCode();
        using var tokens = JsonDocument.Parse(await exchange.Content.ReadAsStringAsync());
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.RootElement.GetProperty("access_token").GetString());
        // Act
        using var result = await browser.SendAsync(request);
        // Assert
        await Assert.That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var identity = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
        await Assert.That(identity.RootElement.GetProperty("contextId").GetString()).IsEqualTo(ContextId);
    }

    private static async Task waitForServerAsync(ChildProcess server, string authority)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (!timeout.IsCancellationRequested)
        {
            if (server.HasExited)
            {
                (await server.CompleteAsync()).EnsureSuccess();
                throw new InvalidOperationException("The service exited before it became ready.");
            }
            try
            {
                using var response = await http.GetAsync(authority + ".well-known/openid-configuration", timeout.Token);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException) { }
            catch (OperationCanceledException) { }
            if (!timeout.IsCancellationRequested)
                await Task.Delay(100, timeout.Token);
        }
        throw new TimeoutException("The HTTPS service did not become ready within 30 seconds.");
    }
}