using Basilisque.Base.Auth.Client.Credentials;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Client.ClientAuth;

internal sealed class RuntimeMachineClientRegistry : IRuntimeMachineClientRegistry, IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly HashSet<string> _fixedNames;
    private readonly OpenIddictClientService _openIddict;
    private bool _disposed;

    /// <summary>
    /// Creates an in-memory registry for the host.
    /// </summary>
    public RuntimeMachineClientRegistry(OpenIddictClientService openIddict, IEnumerable<FixedMachineClientName> fixedNames)
    {
        _openIddict = openIddict;
        _fixedNames = new HashSet<string>(fixedNames.Select(name => name.Value), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void RegisterOrUpdate(string registrationName, Action<MachineClientOptions> configure,
        Func<ClientSigningCredential> createCredential)
    {
        if (string.IsNullOrWhiteSpace(registrationName) || registrationName.Any(character => character is '\u001e' or '\u001f'))
            throw new ArgumentException(Messages.ValidRegistrationNameRequired, nameof(registrationName));
        if (_fixedNames.Contains(registrationName))
            throw new InvalidOperationException(Messages.FixedMachineClientNameExists);
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));
        if (createCredential is null)
            throw new ArgumentNullException(nameof(createCredential));

        var settings = new MachineClientOptions();
        configure(settings);
        if (!settings.IsValid())
            throw new ArgumentException(Messages.InvalidMachineClientOptions, nameof(configure));

        var credential = createCredential() ?? throw new InvalidOperationException(Messages.SigningCredentialMissing);
        Entry entry;
        try
        {
            var registration = new OpenIddictClientRegistration
            {
                RegistrationId = registrationName,
                Issuer = settings.Authority,
                ClientId = settings.ClientId,
                ClientType = ClientTypes.Confidential,
                ConfigurationEndpoint = new Uri(settings.Authority.AbsoluteUri.TrimEnd('/') + "/.well-known/openid-configuration")
            };
            registration.ClientAuthenticationMethods.Add(ClientAuthenticationMethods.PrivateKeyJwt);
            var signing = credential.Credentials;
            var cryptoFactory = signing.CryptoProviderFactory ?? signing.Key.CryptoProviderFactory;
            if (cryptoFactory.GetType() == typeof(CryptoProviderFactory))
            {
                // A cached signature provider can retain a key after its registration has been retired.
                signing.CryptoProviderFactory = new CryptoProviderFactory(cryptoFactory) { CacheSignatureProviders = false };
            }
            registration.SigningCredentials.Add(signing);
            foreach (var scope in settings.Scopes)
                registration.Scopes.Add(scope);
            registration.ConfigurationManager = new ConfigurationManager<OpenIddictConfiguration>(
                registration.ConfigurationEndpoint.AbsoluteUri, new OpenIddictClientRetriever(_openIddict, registration));
            entry = new Entry(registration, credential, settings.CacheAccessTokens, settings.RefreshMargin);
        }
        catch
        {
            credential.Dispose();
            throw;
        }

        Entry? previous;
        bool disposed;
        lock (_gate)
        {
            disposed = _disposed;
            _entries.TryGetValue(registrationName, out previous);
            if (!disposed)
                _entries[registrationName] = entry;
        }
        if (disposed)
        {
            entry.Release();
            throw new ObjectDisposedException(nameof(RuntimeMachineClientRegistry));
        }
        previous?.Release();
    }

    /// <inheritdoc />
    public bool Remove(string registrationName)
    {
        if (string.IsNullOrWhiteSpace(registrationName))
            throw new ArgumentException(Messages.RegistrationNameRequired, nameof(registrationName));

        Entry? entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(registrationName, out entry))
                return false;
            _entries.Remove(registrationName);
        }
        entry.Release();
        return true;
    }

    internal Entry? Acquire(string registrationName)
    {
        lock (_gate)
        {
            if (!_entries.TryGetValue(registrationName, out var entry))
                return null;
            entry.Retain();
            return entry;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Entry[] entries;
        lock (_gate)
        {
            _disposed = true;
            entries = _entries.Values.ToArray();
            _entries.Clear();
        }
        foreach (var entry in entries)
            entry.Release();
    }

    internal sealed class Entry
    {
        private readonly ClientSigningCredential _credential;
        private int _references = 1;

        internal Entry(OpenIddictClientRegistration registration, ClientSigningCredential credential, bool cache, TimeSpan refreshMargin)
        {
            Registration = registration;
            _credential = credential;
            Cache = cache;
            RefreshMargin = refreshMargin;
        }

        internal OpenIddictClientRegistration Registration { get; }
        internal bool Cache { get; }
        internal TimeSpan RefreshMargin { get; }
        internal TokenCacheEntry Tokens { get; } = new();

        internal void Retain() => Interlocked.Increment(ref _references);

        internal void Release()
        {
            if (Interlocked.Decrement(ref _references) != 0)
                return;

            Tokens.Dispose();
            _credential.Dispose();
        }
    }
}
