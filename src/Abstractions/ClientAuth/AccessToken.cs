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

namespace Basilisque.Base.Auth.Abstractions.ClientAuth;

/// <summary>
/// An opaque bearer token and its server-reported expiration date.
/// </summary>
public sealed class AccessToken
{
    /// <summary>
    /// Gets the secret bearer token. Do not log this value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the expiration date when supplied by the authorization server.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Creates an access token. Tokens without expiration metadata are never cached.
    /// </summary>
    public AccessToken(string value, DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(Messages.TokenRequired, nameof(value));

        Value = value;
        ExpiresAt = expiresAt;
    }
}