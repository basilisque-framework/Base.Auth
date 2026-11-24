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
using Basilisque.Base.Auth.Client;
using Basilisque.Base.Auth.Client.Credentials;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Threading;

namespace Basilisque.Base.Auth.Server.Integration.Tests;

/// <summary>Exercises runtime registration against the OpenIddict token endpoint.</summary>
[Category("Integration")]
public sealed class RuntimeMachineClientTests
{
    private const string Name = "partner";
    private const string ReplacementClient = "replacement-client";

    /// <summary>Acquires a token for a registration added to a running provider.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldIssueToken_ForNewRuntimeClient()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        await using var client = environment.CreateRuntimeClients();
        var tokens = client.GetRequiredService<IAccessTokenProvider>();
        var registry = client.GetRequiredService<IRuntimeMachineClientRegistry>();
        register(registry, ProtocolTestEnvironment.ClientId, environment.ClientKey);
        // Act
        var token = await tokens.GetAccessTokenAsync(Name);
        // Assert
        await Assert.That(token.ExpiresAt > DateTimeOffset.UtcNow).IsTrue();
    }

    /// <summary>Checks that replacing a registration discards its cached token.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldUseReplacementCredential_AfterRuntimeUpdate()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        await using var client = environment.CreateRuntimeClients();
        using var replacement = RSA.Create(2048);
        await environment.RegisterAsync(ReplacementClient, ProtocolTestEnvironment.PublicKeys(replacement));
        var tokens = client.GetRequiredService<IAccessTokenProvider>();
        var registry = client.GetRequiredService<IRuntimeMachineClientRegistry>();
        register(registry, ProtocolTestEnvironment.ClientId, environment.ClientKey);
        var original = await tokens.GetAccessTokenAsync(Name);
        register(registry, ReplacementClient, replacement);
        // Act
        var updated = await tokens.GetAccessTokenAsync(Name);
        // Assert
        await Assert.That(updated.Value).IsNotEqualTo(original.Value);
        var jwt = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(updated.Value);
        await Assert.That(jwt.GetClaim("client_id").Value).IsEqualTo(ReplacementClient);
    }

    /// <summary>Checks that removing a registration also removes access to any cached token.</summary>
    [Test]
    public async Task GetAccessTokenAsync_ShouldFail_AfterRuntimeRemoval()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        await using var client = environment.CreateRuntimeClients();
        var tokens = client.GetRequiredService<IAccessTokenProvider>();
        var registry = client.GetRequiredService<IRuntimeMachineClientRegistry>();
        register(registry, ProtocolTestEnvironment.ClientId, environment.ClientKey);
        await tokens.GetAccessTokenAsync(Name);
        registry.Remove(Name);
        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => tokens.GetAccessTokenAsync(Name));
    }

    /// <summary>Checks that requests keep the credential they acquired across concurrent removal.</summary>
    [Test]
    public async Task Remove_ShouldRetainCredential_UntilInFlightRequestFinishes()
    {
        // Arrange
        await using var environment = await ProtocolTestEnvironment.CreateAsync();
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ownership = new CredentialOwnership();
        await using var client = environment.CreateRuntimeClients(async (request, cancellationToken) =>
        {
            if (request.RequestUri?.AbsolutePath == "/connect/token")
            {
                requestStarted.TrySetResult();
                await continueRequest.Task.WaitAsync(cancellationToken);
            }
        });
        var registry = client.GetRequiredService<IRuntimeMachineClientRegistry>();
        var tokens = client.GetRequiredService<IAccessTokenProvider>();
        registry.RegisterOrUpdate(Name, options =>
        {
            options.Authority = ProtocolTestEnvironment.Issuer;
            options.ClientId = ProtocolTestEnvironment.ClientId;
            options.Scopes.Add("api");
        }, () =>
        {
            using var credential = ClientSigningCredential.FromRsa(environment.ClientKey, "client-key");
            return new ClientSigningCredential(credential.Credentials, ownership);
        });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var pending = tokens.GetAccessTokenAsync(Name, timeout.Token);
        await requestStarted.Task.WaitAsync(timeout.Token);
        // Act
        bool disposedDuringRequest;
        try
        {
            registry.Remove(Name);
            disposedDuringRequest = ownership.IsDisposed;
        }
        finally
        {
            continueRequest.TrySetResult();
            await pending;
        }
        // Assert
        await Assert.That(disposedDuringRequest).IsFalse();
        await Assert.That(ownership.IsDisposed).IsTrue();
    }

    private static void register(IRuntimeMachineClientRegistry registry, string clientId, RSA key)
        => registry.RegisterOrUpdate(Name, options =>
        {
            options.Authority = ProtocolTestEnvironment.Issuer;
            options.ClientId = clientId;
            options.Scopes.Add("api");
        }, () => ClientSigningCredential.FromRsa(key, "client-key"));

    private sealed class CredentialOwnership : IDisposable
    {
        internal bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public void Dispose() => IsDisposed = true;
    }
}