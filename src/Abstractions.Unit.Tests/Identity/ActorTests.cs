using Basilisque.Base.Auth.Identity;

namespace Basilisque.Base.Auth.Abstractions.Unit.Tests.Identity;

/// <summary>Tests the product identity contract.</summary>
[Category("Unit")]
public sealed class ActorTests
{
    /// <summary>Rejects explicitly assigned contexts that are blank.</summary>
    [Test]
    public async Task Constructor_ShouldRejectEmptyContext_InServiceIdentity()
    {
        // Arrange
        var context = "";
        // Act / Assert
        await Assert.That(() => new Actor("service-id", BaseAuthClaims.Service, context)).Throws<ArgumentException>();
    }
}
