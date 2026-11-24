<!--
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
-->
# Machine-to-machine authentication

Base.Auth uses the OAuth 2.0 Client Credentials grant with `private_key_jwt` to authenticate
technical clients. The server runs on ASP.NET Core. The client and optional Data Protection
package also support .NET Framework 4.8. The repository's .NET target is configured in
[`Directory.Build.props`](../Directory.Build.props).

## Components

| Package or sample | Responsibility |
|---|---|
| `Basilisque.Base.Auth.Abstractions` | Access token and actor contracts |
| `Basilisque.Base.Auth.Client` | Discovery, signed client assertions, token acquisition, and caching |
| `Basilisque.Base.Auth.Client.Credentials.DataProtection` | Optional protection of RSA PKCS#8 private-key packages |
| `Basilisque.Base.Auth.Server` | Token issuance, API validation, service identities, and client administration |
| Base.Auth persistence modules | Auth database context, OpenIddict stores, persistent replay protection, and database-specific migrations |
| [Reference applications](../samples/README.md) | An ASP.NET Core service and a .NET console client |

## Token flow

1. Provision an RSA key pair for the client. Register its client ID, public JSON Web Key Set
   (JWKS), and optionally an application context with the server. The private key remains with the client.
2. The client uses its configured HTTPS authority to discover the token endpoint at
   `/.well-known/openid-configuration`.
3. The client signs a short-lived assertion and submits it to the token endpoint with the
   Client Credentials request. The server validates the assertion and issues an access token.
4. The client sends the access token as `Authorization: Bearer <token>` to the API. OpenIddict
   validates the token's signature, issuer, audience, type, and lifetime.

Client signing keys, server token credentials, and HTTPS certificates have distinct roles.
Client registration accepts public RSA JWKs. Assertions require `jti`, `iat`, and `exp`,
have a maximum five-minute lifetime, and can be accepted once per client. The database
enforces uniqueness across concurrent requests and server instances. A replay-store
failure blocks token issuance.

The default API scope is `api` and the default access token lifetime is five minutes.
The scope controls admission to the API. Application code enforces any resource-specific
permissions. Access tokens are signed, readable JWTs. Keep secrets out of their claims.

## Integrate a .NET client

```csharp
services.AddBaseAuthClient("sync", options =>
{
    options.Authority = new Uri("https://service.example/");
    options.ClientId = "demo-client";
    options.Scopes.Add("api");
}, provider => LoadSigningCredential(provider));

var token = await tokenProvider.GetAccessTokenAsync("sync", cancellationToken);
```

Implement `LoadSigningCredential` in the host and inject `IAccessTokenProvider` into the
service that calls the API. The credential factory can supply:

- A loaded RSA private key via `ClientSigningCredential.FromRsa(...)`.
- An ECDSA signing key via `ClientSigningCredential.FromEcdsa(...)`. P-256, P-384,
  and P-521 select ES256, ES384, and ES512 respectively.
- An RSA certificate via `ClientSigningCredential.FromCertificate(...)`.
- An installed Windows certificate via
  `CertificateStoreCredentials.Load(thumbprint, StoreLocation.LocalMachine)`.
- A protected package via `IPrivateKeyPackageProtector.Load(package, protectionContext)`.
- Custom `SigningCredentials`, for example with an application-provided crypto provider.

Export the signing key's public registration data with `credential.ExportPublicJwk()`.
For an issuer that expects a JWKS, add the returned key to `new JsonWebKeySet().Keys`.
The export includes the configured algorithm and key ID. Register it with an issuer
that accepts that algorithm for the client. The optional Data Protection package format
supports RSA keys.

The DI container owns the returned credential wrapper. When creating a wrapper, specify
whether it also owns the underlying RSA key or certificate. Grant the service account
access to the private key when using the Windows certificate store. A non-exportable
certificate key can be used directly for signing.

For registrations added with `AddBaseAuthClient`, credentials are loaded per DI container.
Restart the client host to load a replacement key or certificate. The access token cache is per process and registration. It refreshes
tokens before expiry and combines concurrent requests. Set `CacheAccessTokens = false`
for an application that handles renewal itself. Each call then performs a fresh token
exchange. This grant does not issue refresh tokens.

Use the optional `configureOpenIddict` callback for protocol configuration. Keep TLS
validation enabled and keep private keys, assertions, and full tokens out of logs.

### Manage connections at runtime

Call `services.AddBaseAuthRuntimeClients()` when building the host. Inject
`IRuntimeMachineClientRegistry` into the service that manages outbound connections.
The host can start with no connections.

```csharp
registry.RegisterOrUpdate("partner", options =>
{
    options.Authority = new Uri("https://issuer.example/");
    options.ClientId = "sync-client";
    options.Scopes.Add("api");
}, () => LoadPartnerCredential());

var token = await tokenProvider.GetAccessTokenAsync("partner", cancellationToken);
registry.Remove("partner");
```

The application persists the connection settings and credential reference and loads
them into the registry after restart. Each instance maintains its own runtime registry.
Propagate updates and removals to every instance that uses the connection. Restrict
connection management to trusted administrators and apply your deployment's outbound
network policy to discovery and token requests.

Supply a fresh credential wrapper on each registration. The registry owns it and
disposes it after replacement or removal once outstanding requests finish. A replacement
starts with an empty token cache. Calls that already acquired the previous registration
may complete with its token. Removing a registration affects future acquisitions.
Previously issued tokens retain their original lifetime.

### Protect private-key packages

The host configures Data Protection, including a persistent key ring, protection for the
key ring, and application isolation settings. It can then register
`DataProtectionPrivateKeyPackages` as `IPrivateKeyPackageProtector`.

- `Protect(rsa, keyId, protectionContext)` produces a package for storage.
- `Load(package, protectionContext)` opens it with the same key ring and context.

Use a stable protection context unique to the application and client. Back up packages
together with the key ring and its required protection material. Exporting a package
requires an exportable RSA key. On .NET Framework, some CNG certificates permit encrypted
exports only and can refuse the private-parameter export used by this API. Use those
certificates directly for signing.

Multiple client instances can share a protected key or use individual keys under a
single client registration. A registration accepts up to ten keys with distinct `kid`
values. Shared packages require compatible Data Protection settings, a shared key ring,
and access to its protection material on every instance. A process-local token cache
works in either deployment.

## Integrate an ASP.NET Core host

```csharp
services.RegisterServices(); // Host-generated Basilisque registration.
services.AddBaseAuthRelationalPersistence();
services.AddBaseAuthServer(options =>
{
    options.Issuer = new Uri("https://service.example/");
    options.Audience = "product-api";
}, server =>
{
    server.AddSigningCertificate(signingCertificate);
    server.AddEncryptionCertificate(encryptionCertificate);
});
services.AddBaseAuthLocalApi("product-api");
```

Add `UseAuthentication()` and `UseAuthorization()` to the request pipeline and call
`MapBaseAuthTokenEndpoint()` to map the token endpoint. Its default path is
`/connect/token`. OpenIddict serves discovery and public key metadata.

Protect product endpoints with `RequireAuthorization(BaseAuthPolicies.Service)`. Read
the validated service identity through `User.GetBaseAuthActor()`. Registration generates
its `SubjectId` and can assign a `ContextId`. The access token includes `context_id`
when a context was assigned. The application decides how to authorize data access,
for example by checking that context or by looking up the service's tenant permissions
using its `SubjectId`.

Register a custom `IServiceActorFactory` before adding the server defaults to override
actor creation. Other integration points include `IMachineClientRegistry`,
`IClientAssertionReplayStore`, and OpenIddict's store contracts. With custom persistence,
register OpenIddict Core and a durable, atomic replay store in place of
`AddBaseAuthRelationalPersistence()`.

A separate API can call `AddBaseAuthRemoteApi(authority, audience)` to discover the
Base.Auth issuer and its public keys over HTTPS. The service policy also expects the
Base.Auth claim contract and scope.

## Configure persistence

Select `SqlServer`, `PostgreSQL`, or `SQLite` with `Database:Provider` and set
`Database:ConnectionStrings:<Provider>:ConnectionString`. Base.Auth supplies migrations
for `AuthDbContext` for each of these providers and records them in
`__BASAuthMigrationsHistory`, shared with the optional local Identity store. See the
[Basilisque Data Access Entity Framework documentation](https://github.com/basilisque-framework/DataAccess.EntityFramework/wiki)
for custom migration assemblies and provider configuration.

| Table | Purpose |
|---|---|
| `Auth_Applications` | OpenIddict client registrations |
| `Auth_Authorizations` | OpenIddict authorization records |
| `Auth_Scopes` | OpenIddict scope records |
| `Auth_Tokens` | OpenIddict token records |
| `Auth_AssertionReplays` | Base.Auth replay markers for client assertions |

Run migrations explicitly as part of setup. For a cluster, coordinate migration execution
and use a shared SQL Server or PostgreSQL database. All issuer instances need the same
issuer URI and coordinated signing and encryption credentials. SQLite suits a
single-instance installation.

## Run the reference applications

Run these commands from the repository root. The service defaults to SQLite and
`https://localhost:7443` in its Development launch profile. Use a trusted ASP.NET Core
HTTPS development certificate. The host uses dedicated development credentials for
OpenIddict signing and encryption.

1. Prepare a PFX containing a client certificate and its RSA private key. Write the
   corresponding public key as a JWKS with `kty` set to `RSA`, the certificate thumbprint
   as `kid`, and the public modulus and exponent as base64url-encoded `n` and `e`.
2. Initialize the database and register the client in one administrative invocation.

   ```powershell
   dotnet run --project samples/MachineService -f net10.0 -- --initialize-database true --register-client demo-client --public-jwks C:/setup/keys.public.json
   ```

3. Start the service.

   ```powershell
   dotnet run --project samples/MachineService -f net10.0
   ```

4. Set `BASE_AUTH_CLIENT_CERTIFICATE_PASSWORD` in the client process environment and run
   the client. Use `-f net48` to run the .NET Framework variant on Windows.

   ```powershell
   dotnet run --project samples/MachineClient -f net10.0 -- https://localhost:7443/ demo-client C:/setup/client.pfx https://localhost:7443/api/ping api
   ```

Use `--context customer-installation` during registration to assign an application context.
The client prints the subject and any context ID returned by `/api/ping`. A .NET Framework
executable that embeds the library must generate the assembly binding redirects required
by its dependencies.

### Client administration

The reference host runs each administrative command and exits. Registration rejects an
existing client ID.

```powershell
dotnet run --project samples/MachineService -f net10.0 -- --replace-client-keys demo-client --public-jwks C:/setup/keys.public.json
dotnet run --project samples/MachineService -f net10.0 -- --disable-client demo-client
dotnet run --project samples/MachineService -f net10.0 -- --prune-assertions true
```

Key replacement takes the full desired JWKS. For a rolling change, register both keys,
switch the clients, then remove the old key. Rotation preserves the internal subject and
context. Disabling a client blocks new tokens. Existing access tokens remain usable until
they expire.

After recording a new replay marker, the default relational store automatically
prunes expired markers at most once per minute per host. The maintenance command and
`PruneExpiredAssertionsAsync()` can also prune on demand, including during periods
without token requests. Schedule OpenIddict token and authorization pruning separately
where required.

### Deploy the reference host

Build the example container from the repository root. Pass 10.0 or 11.0 as the tag for both the SDK and ASP.NET Core base images.

```powershell
$dotnetVersion = '10.0'
docker build --build-arg "DOTNET_VERSION=$dotnetVersion" -f samples/MachineService/Dockerfile .
```

Outside Development, configure `Auth:Issuer`, `Auth:Audience`,
`Auth:SigningCertificatePath`, and `Auth:EncryptionCertificatePath`. Provide
certificate passwords through deployment secrets when required. Configure HTTPS, the
database provider and connection string, and filesystem permissions for the service account.
Behind a reverse proxy, configure trusted forwarded-header sources in the host.

Use double underscores for nested environment variables, such as `Auth__Issuer`.
The container runs as an unprivileged user. For SQLite, persist `/data` and grant the
container user write access. Mount private keys or supply them through a credential
integration.

## References

- [OpenIddict assertion-based client authentication](https://documentation.openiddict.com/configuration/assertion-based-client-authentication)
- [OpenIddict server integration](https://documentation.openiddict.com/guides/getting-started/creating-your-own-server-instance)
- [ASP.NET Core Data Protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview)