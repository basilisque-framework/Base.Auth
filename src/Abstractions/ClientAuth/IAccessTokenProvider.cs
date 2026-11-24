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
/// Obtains access tokens for explicitly configured technical clients.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets a usable token, renewing it when necessary. Never creates a user session.
    /// </summary>
    /// <remarks>Client configuration determines whether valid tokens are reused or every call performs a new exchange.</remarks>
    Task<AccessToken> GetAccessTokenAsync(string registrationName, CancellationToken cancellationToken = default);
}