/*
   Copyright 2026 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Basilisque.Base.Auth.Abstractions.ClientAuth;
using Basilisque.Base.Auth.Client.ClientAuth;
using Microsoft.Extensions.Options;

namespace Basilisque.Base.Auth.Client.Unit.Tests.ClientAuth;

/// <summary>Tests renewal and concurrent access without mocking OAuth protocol internals.</summary>
[Category("Unit")]
public sealed class CachedAccessTokenProviderTests : IDisposable
{
    private readonly ITokenEndpointClient _endpoint = Substitute.For<ITokenEndpointClient>();
    private readonly CachedAccessTokenProvider _provider;
    private readonly MachineClientOptions _settings = new() { Authority = new Uri("https://issuer.example/"), ClientId = "client" };

    /// <summary>Creates isolated cache state for each test.</summary>
    public CachedAccessTokenProviderTests()
    {
        var options = Substitute.For<IOptionsMonitor<MachineClientOptions>>();
        options.Get("test").Returns(_settings);
        _provider = new CachedAccessTokenProvider(_endpoint, options);
    }

    /// <summary>Shares one acquisition across parallel requests.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldAcquireOnce_InConcurrentRequests()
    {
        // Arrange
        _endpoint.RequestAsync("test", Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(20);
                return new AccessToken("token", DateTimeOffset.UtcNow.AddMinutes(5));
            });
        // Act
        var tokens = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => _provider.GetAccessTokenAsync("test")));
        // Assert
        await Assert.That(tokens.All(token => token.Value == "token")).IsTrue();
        await _endpoint.Received(1).RequestAsync("test", Arg.Any<CancellationToken>());
    }

    /// <summary>Renews tokens before their expiration.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldRenew_InsideRefreshMargin()
    {
        // Arrange
        _endpoint.RequestAsync("test", Arg.Any<CancellationToken>()).Returns(
            new AccessToken("first", DateTimeOffset.UtcNow.AddSeconds(10)),
            new AccessToken("second", DateTimeOffset.UtcNow.AddMinutes(5)));
        await _provider.GetAccessTokenAsync("test");
        // Act
        var token = await _provider.GetAccessTokenAsync("test");
        // Assert
        await Assert.That(token.Value).IsEqualTo("second");
    }

    /// <summary>Never caches missing expiration metadata.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldRequestAgain_WithoutExpirationMetadata()
    {
        // Arrange
        _endpoint.RequestAsync("test", Arg.Any<CancellationToken>()).Returns(new AccessToken("token", null));
        await _provider.GetAccessTokenAsync("test");
        // Act
        await _provider.GetAccessTokenAsync("test");
        // Assert
        await _endpoint.Received(2).RequestAsync("test", Arg.Any<CancellationToken>());
    }

    /// <summary>Allows recovery after a failed exchange.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldRetry_AfterFailedAcquisition()
    {
        // Arrange
        _endpoint.RequestAsync("test", Arg.Any<CancellationToken>()).Returns(
            Task.FromException<AccessToken>(new InvalidOperationException("unavailable")),
            Task.FromResult(new AccessToken("recovered", DateTimeOffset.UtcNow.AddMinutes(5))));
        try
        {
            await _provider.GetAccessTokenAsync("test");
        }
        catch (InvalidOperationException) { }
        // Act
        var token = await _provider.GetAccessTokenAsync("test");
        // Assert
        await Assert.That(token.Value).IsEqualTo("recovered");
    }

    /// <summary>Does not silently use an already expired response.</summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task GetAccessTokenAsync_ShouldRejectExpiredToken_InServerResponse(bool cacheAccessTokens)
    {
        // Arrange
        _settings.CacheAccessTokens = cacheAccessTokens;
        _endpoint.RequestAsync("test", Arg.Any<CancellationToken>()).Returns(new AccessToken("expired", DateTimeOffset.UtcNow.AddSeconds(-1)));
        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.GetAccessTokenAsync("test"));
    }

    /// <summary>Requests a fresh token on each call when the application owns token caching.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldRequestFreshToken_WhenCachingDisabled()
    {
        // Arrange
        _settings.CacheAccessTokens = false;
        _endpoint.RequestAsync("test", Arg.Any<CancellationToken>()).Returns(
            new AccessToken("first", DateTimeOffset.UtcNow.AddMinutes(5)),
            new AccessToken("second", DateTimeOffset.UtcNow.AddMinutes(5)));
        await _provider.GetAccessTokenAsync("test");
        // Act
        var token = await _provider.GetAccessTokenAsync("test");
        // Assert
        await Assert.That(token.Value).IsEqualTo("second");
        await _endpoint.Received(2).RequestAsync("test", Arg.Any<CancellationToken>());
    }

    /// <summary>Honors a caller cancellation before starting network work.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldCancel_BeforeNetworkRequest()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        // Act / Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _provider.GetAccessTokenAsync("test", cancellation.Token));
        await _endpoint.DidNotReceive().RequestAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <inheritdoc />
    public void Dispose() => _provider.Dispose();
}