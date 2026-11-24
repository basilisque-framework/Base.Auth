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