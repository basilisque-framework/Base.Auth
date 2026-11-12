using Basilisque.Base.Auth.Server.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Basilisque.Base.Auth.Server.ClientAuth;

internal sealed class MachineClientRegistry : IMachineClientRegistry
{
    internal const string SubjectProperty = "base_auth.subject";
    internal const string ContextProperty = "base_auth.context";
    private readonly IOpenIddictApplicationManager _applications;
    private readonly IOptions<AuthServerOptions> _options;

    /// <summary>
    /// Creates the registry using the application manager and auth server settings.
    /// </summary>
    public MachineClientRegistry(IOpenIddictApplicationManager applications, IOptions<AuthServerOptions> options)
    {
        _applications = applications;
        _options = options;
    }

    public async ValueTask RegisterAsync(string clientId, string contextId, JsonWebKeySet publicKeys, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contextId);
        validatePublicKeys(publicKeys);
        if (await _applications.FindByClientIdAsync(clientId, cancellationToken) is not null)
            throw new InvalidOperationException("The client already exists. Use an explicit key rotation operation.");

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = ClientTypes.Confidential,
            DisplayName = clientId,
            JsonWebKeySet = publicKeys
        };
        descriptor.Permissions.UnionWith([
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.ClientCredentials,
            Permissions.Prefixes.Scope + _options.Value.ApiScope
        ]);
        descriptor.Properties[SubjectProperty] = JsonSerializer.SerializeToElement(Guid.NewGuid().ToString("D"), MachineClientJsonContext.Default.String);
        descriptor.Properties[ContextProperty] = JsonSerializer.SerializeToElement(contextId, MachineClientJsonContext.Default.String);
        await _applications.CreateAsync(descriptor, cancellationToken);
    }

    public async ValueTask ReplacePublicKeysAsync(string clientId, JsonWebKeySet publicKeys, CancellationToken cancellationToken = default)
    {
        validatePublicKeys(publicKeys);
        var application = await findAsync(clientId, cancellationToken);
        var descriptor = new OpenIddictApplicationDescriptor();
        await _applications.PopulateAsync(descriptor, application, cancellationToken);
        descriptor.JsonWebKeySet = publicKeys;
        await _applications.UpdateAsync(application, descriptor, cancellationToken);
    }

    public async ValueTask DisableAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var application = await findAsync(clientId, cancellationToken);
        var descriptor = new OpenIddictApplicationDescriptor();
        await _applications.PopulateAsync(descriptor, application, cancellationToken);
        descriptor.Permissions.Remove(Permissions.Endpoints.Token);
        await _applications.UpdateAsync(application, descriptor, cancellationToken);
    }

    private async ValueTask<object> findAsync(string clientId, CancellationToken cancellationToken) =>
        await _applications.FindByClientIdAsync(clientId, cancellationToken)
        ?? throw new InvalidOperationException("The client does not exist.");

    private static void validatePublicKeys(JsonWebKeySet publicKeys)
    {
        ArgumentNullException.ThrowIfNull(publicKeys);
        if (publicKeys.Keys.Count == 0 || publicKeys.Keys.Count > 10)
            throw new ArgumentException("Between one and ten public keys are required.", nameof(publicKeys));
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in publicKeys.Keys)
        {
            if (key.HasPrivateKey || !string.IsNullOrEmpty(key.D) || !string.IsNullOrEmpty(key.P) ||
                !string.IsNullOrEmpty(key.Q) || !string.IsNullOrEmpty(key.DP) || !string.IsNullOrEmpty(key.DQ) ||
                !string.IsNullOrEmpty(key.QI) || !string.IsNullOrEmpty(key.K) ||
                key.Kty != JsonWebAlgorithmsKeyTypes.RSA && key.Kty != JsonWebAlgorithmsKeyTypes.EllipticCurve ||
                string.IsNullOrWhiteSpace(key.Kid) || !identifiers.Add(key.Kid))
                throw new ArgumentException("Only public RSA/EC keys with distinct key identifiers are accepted.", nameof(publicKeys));
            if (key.Kty == JsonWebAlgorithmsKeyTypes.RSA &&
                (string.IsNullOrEmpty(key.N) || string.IsNullOrEmpty(key.E) || Base64UrlEncoder.DecodeBytes(key.N).Length < 256))
                throw new ArgumentException("An RSA public key with at least 2048 bits is required.", nameof(publicKeys));
            if (key.Kty == JsonWebAlgorithmsKeyTypes.EllipticCurve &&
                (key.Crv is not ("P-256" or "P-384" or "P-521") || string.IsNullOrEmpty(key.X) || string.IsNullOrEmpty(key.Y)))
                throw new ArgumentException("A supported EC public key is required.", nameof(publicKeys));
            if (key.Kty == JsonWebAlgorithmsKeyTypes.RSA)
            {
                using var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = Base64UrlEncoder.DecodeBytes(key.N),
                    Exponent = Base64UrlEncoder.DecodeBytes(key.E)
                });
                if (rsa.KeySize < 2048)
                    throw new ArgumentException("An RSA public key with at least 2048 bits is required.", nameof(publicKeys));
            }
        }
    }
}

[JsonSerializable(typeof(string))]
internal sealed partial class MachineClientJsonContext : JsonSerializerContext
{
}
