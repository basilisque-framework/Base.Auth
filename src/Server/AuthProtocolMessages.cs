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