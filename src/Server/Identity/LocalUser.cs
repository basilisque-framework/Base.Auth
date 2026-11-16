using Microsoft.AspNetCore.Identity;

namespace Basilisque.Base.Auth.Server.Identity;

/// <summary>
/// A local sign-in account assigned to one application context.
/// </summary>
public sealed class LocalUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the context assigned during trusted provisioning.
    /// </summary>
    public required string ContextId { get; set; }

    /// <summary>
    /// Gets or sets whether the user can sign in to the product.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
