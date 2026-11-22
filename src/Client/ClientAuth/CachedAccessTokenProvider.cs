using Basilisque.Base.Auth.ClientAuth;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class CachedAccessTokenProvider : IAccessTokenProvider, IDisposable
{
    private readonly ConcurrentDictionary<string, TokenCacheEntry> _entries = new(StringComparer.Ordinal);
    private readonly ITokenEndpointClient _client;
    private readonly IOptionsMonitor<MachineClientOptions> _options;
    private readonly RuntimeMachineClientRegistry? _runtime;

    /// <summary>
    /// Creates a token cache backed by the token endpoint and named client options.
    /// </summary>
    public CachedAccessTokenProvider(ITokenEndpointClient client, IOptionsMonitor<MachineClientOptions> options,
        RuntimeMachineClientRegistry? registry = null)
    {
        _client = client;
        _options = options;
        _runtime = registry;
    }

    public async Task<AccessToken> GetAccessTokenAsync(string registrationName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registrationName))
            throw new ArgumentException(Messages.RegistrationNameRequired, nameof(registrationName));

        cancellationToken.ThrowIfCancellationRequested();

        var runtime = _runtime?.Acquire(registrationName);
        if (runtime is not null)
        {
            try
            {
                Task<AccessToken> requestRuntime() => requestTokenAsync(() => _client.RequestAsync(registrationName, cancellationToken, runtime.Registration));
                return runtime.Cache
                    ? await runtime.Tokens.GetAsync(requestRuntime, runtime.RefreshMargin, cancellationToken).ConfigureAwait(false)
                    : await requestRuntime().ConfigureAwait(false);
            }
            finally
            {
                runtime.Release();
            }
        }

        // Named options validate before creating cache entries, so arbitrary names cannot grow the cache.
        var settings = _options.Get(registrationName);
        if (!settings.IsValid())
            throw new InvalidOperationException(Messages.MachineClientNotConfigured);

        Task<AccessToken> request() => requestTokenAsync(() => _client.RequestAsync(registrationName, cancellationToken));
        return settings.CacheAccessTokens
            ? await _entries.GetOrAdd(registrationName, _ => new TokenCacheEntry())
                .GetAsync(request, settings.RefreshMargin, cancellationToken).ConfigureAwait(false)
            : await request().ConfigureAwait(false);
    }

    private static async Task<AccessToken> requestTokenAsync(Func<Task<AccessToken>> request)
    {
        var token = await request().ConfigureAwait(false);

        if (token.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException(Messages.ExpiredTokenReturned);

        return token;
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
            entry.Dispose();
    }
}
