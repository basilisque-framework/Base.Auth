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