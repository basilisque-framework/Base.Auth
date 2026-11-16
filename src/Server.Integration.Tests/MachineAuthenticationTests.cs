using Basilisque.Base.Auth.ClientAuth;
using Basilisque.Base.Auth.Client.Credentials;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Basilisque.Base.Auth.Server.Integration.Tests;

/// <summary>Exercises actual OpenIddict client/server processing against a migrated SQLite database.</summary>
[Category("Integration")]
public sealed class MachineAuthenticationTests
{
    /// <summary>Obtains a token using a generated public ECDSA registration and the matching private key.</summary>
    [Test]
    [Arguments(256, "ES256")]
    [Arguments(384, "ES384")]
    [Arguments(521, "ES512")]
    public async Task GetAccessTokenAsync_ShouldIssueToken_WithEcdsaCredential(int keySize, string algorithm)
    {
        // Arrange
        const string ClientId = "ecdsa-client";
        const string KeyId = "ecdsa-key";
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        using var key = ECDsa.Create(keySize switch
        {
            256 => ECCurve.NamedCurves.nistP256,
            384 => ECCurve.NamedCurves.nistP384,
            521 => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentOutOfRangeException(nameof(keySize))
        });
        using var credential = ClientSigningCredential.FromEcdsa(key, KeyId);
        var keys = new JsonWebKeySet();
        var publicKey = credential.ExportPublicJwk();
        if (publicKey.HasPrivateKey)
            throw new InvalidOperationException("The exported JWK contains private key material.");
        keys.Keys.Add(publicKey);
        await environment.RegisterAsync(ClientId, keys);
        await using var client = environment.CreateClient(ClientId, () => ClientSigningCredential.FromEcdsa(key, KeyId));
        // Act
        var token = await client.GetRequiredService<IAccessTokenProvider>().GetAccessTokenAsync("test");
        // Assert
        await Assert.That(string.IsNullOrWhiteSpace(token.Value)).IsFalse();
        await Assert.That(keys.Keys.Single().Alg).IsEqualTo(algorithm);
    }

    /// <summary>Obtains independent server-issued tokens when the calling application manages caching.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldIssueFreshToken_WhenCachingDisabled()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        await using var client = environment.CreateClient(cacheAccessTokens: false);
        var tokens = client.GetRequiredService<IAccessTokenProvider>();
        var first = await tokens.GetAccessTokenAsync("test");
        // Act
        var second = await tokens.GetAccessTokenAsync("test");
        // Assert
        await Assert.That(second.Value).IsNotEqualTo(first.Value);
        await Assert.That(second.ExpiresAt > DateTimeOffset.UtcNow).IsTrue();
    }

    /// <summary>Obtains a token through discovery and private_key_jwt and calls the protected product API.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldAuthenticateProductService_WithRegisteredKey()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        await using var client = environment.CreateClient();
        var token = await client.GetRequiredService<IAccessTokenProvider>().GetAccessTokenAsync("test");
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        // Act
        using var response = await environment.Http.SendAsync(request);
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var identity = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        await Assert.That(identity.RootElement.GetProperty("contextId").GetString()).IsEqualTo(ProtocolTestEnvironment.ContextId);
        await Assert.That(identity.RootElement.GetProperty("kind").GetString()).IsEqualTo("service");
        await Assert.That(identity.RootElement.GetProperty("subjectId").GetString()).IsNotEqualTo(ProtocolTestEnvironment.ClientId);
    }

    /// <summary>Rejects assertions signed with an unregistered key.</summary>
    [Test]
    public async Task ExchangeAsync_ShouldRejectAssertion_WithWrongPrivateKey()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        using var wrongKey = RSA.Create(2048);
        var assertion = environment.CreateAssertion(key: wrongKey);
        // Act
        using var response = await environment.ExchangeAsync(assertion);
        // Assert
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
    }

    /// <summary>Rejects an assertion issued for another server.</summary>
    [Test]
    public async Task ExchangeAsync_ShouldRejectAssertion_WithWrongAudience()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        var assertion = environment.CreateAssertion(audience: "https://other.example/");
        // Act
        using var response = await environment.ExchangeAsync(assertion);
        // Assert
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
    }

    /// <summary>Rejects a previously consumed assertion even though its signature is still valid.</summary>
    [Test]
    public async Task ExchangeAsync_ShouldRejectReplay_AfterSuccessfulExchange()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        var assertion = environment.CreateAssertion();
        using var first = await environment.ExchangeAsync(assertion);
        first.EnsureSuccessStatusCode();
        // Act
        using var response = await environment.ExchangeAsync(assertion);
        // Assert
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
    }

    /// <summary>Consumes an assertion once across concurrent request scopes.</summary>
    [Test]
    public async Task ExchangeAsync_ShouldAcceptOnlyOnce_InConcurrentAssertionReplay()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        var assertion = environment.CreateAssertion();
        // Act
        var responses = await Task.WhenAll(environment.ExchangeAsync(assertion), environment.ExchangeAsync(assertion));
        // Assert
        try { await Assert.That(responses.Count(response => response.IsSuccessStatusCode)).IsEqualTo(1); }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    /// <summary>Prevents new tokens for disabled registrations.</summary>
    [Test]
    public async Task ExchangeAsync_ShouldRejectClient_AfterAdministrativeDisable()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        await environment.DisableAsync();
        // Act
        using var response = await environment.ExchangeAsync(environment.CreateAssertion());
        // Assert
        await Assert.That(response.IsSuccessStatusCode).IsFalse();
    }

    /// <summary>Preserves the internal identity when client signing credentials change.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldPreserveSubject_AfterKeyRotation()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        var before = new JsonWebToken(await environment.GetTokenAsync());
        using var newKey = RSA.Create(2048);
        await environment.RotateKeyAsync(newKey);
        await using var client = environment.CreateClient(newKey);
        // Act
        var after = await client.GetRequiredService<IAccessTokenProvider>().GetAccessTokenAsync("test");
        // Assert
        await Assert.That(new JsonWebToken(after.Value).Subject).IsEqualTo(before.Subject);
    }

    /// <summary>Rejects private key material at the server's administrative boundary.</summary>
    [Test]
    public async Task RegisterAsync_ShouldRejectPrivateKeys_InPublicKeyRegistration()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        using var key = RSA.Create(2048);
        var keys = new JsonWebKeySet();
        keys.Keys.Add(JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(key.ExportParameters(true)) { KeyId = "private-key" }));
        // Act / Assert
        await Assert.ThrowsAsync<ArgumentException>(() => environment.RegisterAsync("another-client", keys));
    }

    /// <summary>Requires access tokens at the API boundary.</summary>
    [Test]
    public async Task SendAsync_ShouldRejectAnonymousRequest_AtProtectedApi()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        // Act
        using var response = await environment.Http.GetAsync("api/ping");
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    /// <summary>Does not accept client assertions as bearer tokens.</summary>
    [Test]
    public async Task SendAsync_ShouldRejectClientAssertion_AtProtectedApi()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", environment.CreateAssertion());
        // Act
        using var response = await environment.Http.SendAsync(request);
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    /// <summary>Checks issuer, audience, type, lifetime and signature independently.</summary>
    [Test]
    [Arguments("issuer")]
    [Arguments("audience")]
    [Arguments("type")]
    [Arguments("expired")]
    [Arguments("signature")]
    public async Task SendAsync_ShouldRejectInvalidToken_AtProtectedApi(string defect)
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        using var otherKey = RSA.Create(2048);
        var token = environment.CreateApiToken(
            issuer: defect == "issuer" ? "https://other.example/" : ProtocolTestEnvironment.Issuer.AbsoluteUri,
            audience: defect == "audience" ? "other-api" : ProtocolTestEnvironment.Audience,
            tokenType: defect == "type" ? "JWT" : "at+jwt",
            expires: defect == "expired" ? DateTime.UtcNow.AddMinutes(-1) : null,
            signingKey: defect == "signature" ? otherKey : null);
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Act
        using var response = await environment.Http.SendAsync(request);
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
