using Microsoft.AspNetCore.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// A local sign-in account with an optional application context.
/// </summary>
public sealed class LocalUser : IdentityUser<Guid>
{
    /// <summary>Creates a local account with a UUIDv7 identifier.</summary>
    public LocalUser()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Gets or sets the context assigned during trusted provisioning.
    /// </summary>
    public string? ContextId { get; set; }

    /// <summary>
    /// Gets or sets whether the user can sign in to the product.
    /// </summary>
    public bool Enabled { get; set; } = true;
}