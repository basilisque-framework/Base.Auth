namespace Basilisque.Base.Auth.Authorization;

/// <summary>Defines the effect of an explicit role permission rule.</summary>
public enum PermissionEffect
{
    /// <summary>Allows an action in the absence of an applicable denial.</summary>
    Allow = 1,

    /// <summary>Denies an action even when a separate applicable rule allows it.</summary>
    Deny = 2
}