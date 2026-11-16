using Basilisque.Base.Auth.Server.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Server.Integration.Tests;

/// <summary>Exercises Identity persistence and user authorization through the real OpenIddict endpoints.</summary>
[Category("Integration")]
public sealed class LocalUserTests
{
    internal const string Email = "reader@example.test";
    internal const string Password = "LocalIntegration!123";
    private const string ClientId = "user-client";
    private const string Redirect = "https://client.example/callback";
    private const string Verifier = "test-code-verifier-with-at-least-43-characters";

    /// <summary>Checks that the token carries the admitted internal identity.</summary>
    [Test]
    public async Task ExchangeCodeAsync_ShouldIssueUserToken_ForProvisionedLocalUser()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync(localUsers: true);
        var userId = await provisionAsync(environment);
        var code = await authorizeAsync(environment);
        // Act
        using var response = await exchangeAsync(environment, code, Verifier);
        // Assert
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK).Because(body);
        using var tokens = JsonDocument.Parse(body);
        var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(tokens.RootElement.GetProperty("access_token").GetString());
        await Assert.That(jwt.Subject).IsEqualTo(userId);
        await Assert.That(jwt.GetClaim("actor_type").Value).IsEqualTo("user");
        await Assert.That(jwt.TryGetClaim("base_auth.local_security_stamp", out _)).IsFalse();
    }

    /// <summary>Checks that a browser login cannot bypass the code verifier.</summary>
    [Test]
    public async Task ExchangeCodeAsync_ShouldRejectWrongVerifier_ForAuthorizationCode()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync(localUsers: true);
        await provisionAsync(environment);
        var code = await authorizeAsync(environment);
        // Act
        using var response = await exchangeAsync(environment, code, Verifier + "-wrong");
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    /// <summary>Checks that disabling a user invalidates a pending code.</summary>
    [Test]
    public async Task ExchangeCodeAsync_ShouldRejectCode_WhenAccountWasDisabled()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync(localUsers: true);
        var userId = await provisionAsync(environment);
        var code = await authorizeAsync(environment);
        await using var scope = environment.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        var user = await users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Test user is missing.");
        user.Enabled = false;
        await ensureSuccessAsync(users.UpdateSecurityStampAsync(user));
        // Act
        using var response = await exchangeAsync(environment, code, Verifier);
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    /// <summary>Checks that an application cookie alone cannot authorize a bearer API call.</summary>
    [Test]
    public async Task UserPolicy_ShouldRejectCookie_WhenAccessTokenIsMissing()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync(localUsers: true);
        await provisionAsync(environment);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Add("Cookie", await signInAsync(environment));
        // Act
        using var response = await environment.Http.SendAsync(request);
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    /// <summary>Checks that password failures persist and lock out the account.</summary>
    [Test]
    public async Task PasswordSignInAsync_ShouldRejectCorrectPassword_AfterRepeatedFailures()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync(localUsers: true);
        await provisionAsync(environment);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failure = await environment.Http.PostAsync("/test/sign-in?wrong-password=true", content: null);
        }
        // Act
        using var response = await environment.Http.PostAsync("/test/sign-in", content: null);
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private static async Task<string> provisionAsync(ProtocolTestEnvironment environment)
    {
        await using var scope = environment.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<LocalUser>>();
        var user = new LocalUser { UserName = Email, Email = Email, ContextId = ProtocolTestEnvironment.ContextId };
        await ensureSuccessAsync(users.CreateAsync(user, Password));
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = ClientId, ClientType = ClientTypes.Public, ConsentType = ConsentTypes.Implicit
        };
        descriptor.RedirectUris.Add(new Uri(Redirect));
        descriptor.Permissions.UnionWith([Permissions.Endpoints.Authorization, Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode, Permissions.ResponseTypes.Code, Permissions.Prefixes.Scope + "api"]);
        await scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>().CreateAsync(descriptor);
        return user.Id;
    }

    private static async Task<string> authorizeAsync(ProtocolTestEnvironment environment)
    {
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(Verifier)));
        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("/connect/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = ClientId, ["redirect_uri"] = Redirect, ["response_type"] = "code", ["scope"] = "openid api",
            ["code_challenge"] = challenge, ["code_challenge_method"] = "S256", ["state"] = "test-state", ["nonce"] = "test-nonce"
        }));
        request.Headers.Add("Cookie", await signInAsync(environment));
        using var response = await environment.Http.SendAsync(request);
        var location = response.Headers.Location ?? throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        var parameters = QueryHelpers.ParseQuery(location.Query);
        if (!parameters.TryGetValue("code", out var code))
            throw new InvalidOperationException("Authorization did not return a code. " + location);
        return code.ToString();
    }

    private static async Task<string> signInAsync(ProtocolTestEnvironment environment)
    {
        using var response = await environment.Http.PostAsync("/test/sign-in", content: null);
        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0])
            .Single(value => value.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal));
    }

    private static Task<HttpResponseMessage> exchangeAsync(ProtocolTestEnvironment environment, string code, string verifier)
        => environment.Http.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["client_id"] = ClientId, ["redirect_uri"] = Redirect,
            ["code"] = code, ["code_verifier"] = verifier
        }));

    private static async Task ensureSuccessAsync(Task<IdentityResult> operation)
    {
        var result = await operation;
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
