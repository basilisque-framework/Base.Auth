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

using Basilisque.Base.Auth.Server.Extensions;
using Basilisque.Base.Auth.Server.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Basilisque.Base.Auth.Server.Unit.Tests;

/// <summary>Verifies default registration does not replace an application identity mapper.</summary>
[Category("Unit")]
public sealed class ExtensionTests
{
    /// <summary>Preserves a product mapper supplied before the Base.Auth defaults.</summary>
    [Test]
    public async Task AddBaseAuthServer_ShouldPreserveProductMapper_InExplicitRegistration()
    {
        // Arrange
        var mapper = Substitute.For<IServiceActorFactory>();
        var services = new ServiceCollection();
        services.AddSingleton(mapper);
        services.AddBaseAuthServer(options =>
        {
            options.Issuer = new Uri("https://issuer.example/");
            options.Audience = "api";
        },
            options => options.AddEphemeralSigningKey().AddEphemeralEncryptionKey());
        using var provider = services.BuildServiceProvider();
        // Act
        var resolved = provider.GetRequiredService<IServiceActorFactory>();
        // Assert
        await Assert.That(ReferenceEquals(mapper, resolved)).IsTrue();
    }
}