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

using Basilisque.Base.Auth.Abstractions.ClientAuth;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class TokenCacheEntry : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AccessToken? _token;

    internal async Task<AccessToken> GetAsync(Func<Task<AccessToken>> request, TimeSpan refreshMargin, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_token?.ExpiresAt > DateTimeOffset.UtcNow.Add(refreshMargin))
                return _token;

            _token = null;
            var token = await request().ConfigureAwait(false);
            if (token.ExpiresAt.HasValue)
                _token = token;

            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();
}