namespace Basilisque.Base.Auth.DataAccess.EntityFramework.Relational.Model;

/// <summary>A one-time-use marker for a validated client assertion.</summary>
public sealed class AssertionReplayEntry
{
    /// <summary>Gets or sets the SHA-256 identifier derived from client ID and JWT ID.</summary>
    public string Id { get; set; } = "";
    /// <summary>Gets or sets the expiration in UTC ticks for portable indexed pruning.</summary>
    public long ExpiresAtUtcTicks { get; set; }
}
