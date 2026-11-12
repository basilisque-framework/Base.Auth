using Basilisque.Base.Auth.ClientAuth;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class CachedAccessTokenProvider : IAccessTokenProvider, IDisposable
{
    private sealed class Entry
    {
        internal readonly SemaphoreSlim Gate = new(1, 1);
        internal AccessToken? Token;
    }

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly ITokenEndpointClient _client;
    private readonly IOptionsMonitor<MachineClientOptions> _options;

    /// <summary>
    /// Creates a token cache backed by the token endpoint and named client options.
    /// </summary>
    public CachedAccessTokenProvider(ITokenEndpointClient client, IOptionsMonitor<MachineClientOptions> options)
    {
        _client = client;
        _options = options;
    }

    public async Task<AccessToken> GetAccessTokenAsync(string registrationName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registrationName))
            throw new ArgumentException("A registration name is required.", nameof(registrationName));

        cancellationToken.ThrowIfCancellationRequested();

        // Named options validate before creating cache entries, so arbitrary names cannot grow the cache.
        var settings = _options.Get(registrationName);
        if (!settings.IsValid())
            throw new InvalidOperationException("The machine client is not configured.");

        if (!settings.CacheAccessTokens)
            return await requestTokenAsync(registrationName, cancellationToken).ConfigureAwait(false);

        var entry = _entries.GetOrAdd(registrationName, _ => new Entry());
        await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (entry.Token?.ExpiresAt > DateTimeOffset.UtcNow.Add(settings.RefreshMargin))
                return entry.Token;

            entry.Token = null;

            var token = await requestTokenAsync(registrationName, cancellationToken).ConfigureAwait(false);

            if (token.ExpiresAt.HasValue)
                entry.Token = token;

            return token;
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private async Task<AccessToken> requestTokenAsync(string registrationName, CancellationToken cancellationToken)
    {
        var token = await _client.RequestAsync(registrationName, cancellationToken).ConfigureAwait(false);

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("The authorization server returned an expired token.");

        return token;
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
            entry.Gate.Dispose();
    }
}
