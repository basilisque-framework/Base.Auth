namespace Basilisque.Base.Auth.Server;

/// <summary>Identifies the localizer and stable keys for Base.Auth protocol error descriptions.</summary>
public sealed class AuthProtocolMessages
{
    private AuthProtocolMessages()
    {
    }

    /// <summary>The account no longer qualifies for authorization.</summary>
    public const string AccountNoLongerAuthorized = nameof(AccountNoLongerAuthorized);

    /// <summary>The client assertion could not be accepted.</summary>
    public const string ClientAssertionInvalidOrReplayed = nameof(ClientAssertionInvalidOrReplayed);

    /// <summary>The client is not provisioned for local sign-in.</summary>
    public const string ClientNotProvisionedForFirstPartySignIn = nameof(ClientNotProvisionedForFirstPartySignIn);

    /// <summary>Interactive consent is unavailable.</summary>
    public const string InteractiveConsentUnavailable = nameof(InteractiveConsentUnavailable);

    /// <summary>The authorization code could not be accepted.</summary>
    public const string InvalidAuthorizationCode = nameof(InvalidAuthorizationCode);

    /// <summary>The account lacks access to the product.</summary>
    public const string ProductAccessDenied = nameof(ProductAccessDenied);

    /// <summary>The request requires sign-in.</summary>
    public const string SignInRequired = nameof(SignInRequired);

    internal static IReadOnlyList<string> Keys { get; } =
    [
        AccountNoLongerAuthorized, ClientAssertionInvalidOrReplayed, ClientNotProvisionedForFirstPartySignIn,
        InteractiveConsentUnavailable, InvalidAuthorizationCode, ProductAccessDenied, SignInRequired
    ];
}