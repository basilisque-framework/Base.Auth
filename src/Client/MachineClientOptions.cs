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

namespace Basilisque.Base.Auth.Client;

/// <summary>
/// Stable connection settings, independent of product roles and key storage.
/// </summary>
public sealed class MachineClientOptions
{
    /// <summary>
    /// Gets or sets the HTTPS issuer URL used for discovery.
    /// </summary>
    public Uri Authority { get; set; } = null!;

    /// <summary>
    /// Gets or sets the registered technical client identifier.
    /// </summary>
    public string ClientId { get; set; } = "";

    /// <summary>
    /// Gets the API scopes requested by this registration.
    /// </summary>
    public ICollection<string> Scopes { get; } = [];

    /// <summary>
    /// Gets or sets whether valid access tokens are reused. Defaults to true; disable when the caller manages token caching.
    /// </summary>
    public bool CacheAccessTokens { get; set; } = true;

    /// <summary>
    /// Gets or sets the renewal margin. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan RefreshMargin { get; set; } = TimeSpan.FromSeconds(30);

    internal bool IsValid() =>
        Authority is { IsAbsoluteUri: true } && Authority.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(Authority.Query) && string.IsNullOrEmpty(Authority.Fragment) &&
        string.IsNullOrEmpty(Authority.UserInfo) && !string.IsNullOrWhiteSpace(ClientId) &&
        RefreshMargin >= TimeSpan.Zero && RefreshMargin <= TimeSpan.FromMinutes(5) &&
        Scopes.All(scope => !string.IsNullOrWhiteSpace(scope) && !scope.Any(char.IsWhiteSpace));
}