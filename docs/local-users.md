# Local user authentication

ASP.NET Core Identity handles local credentials, password hashing, lockout, account
recovery tokens, and authenticator-based two-factor sign-in. OpenIddict handles the
authorization request, code exchange, and token issuance. The local admission service
resolves the account to a stable product user ID and, when assigned, an application context.

## Integrate the host

Register [Base.Auth's OpenIddict server and API validation](m2m.md#integrate-an-aspnet-core-host)
and apply the database migrations for the selected provider. Enable the local account
module and choose an Identity store and a login UI.

```csharp
services.AddBaseAuthLocalUsers()
    .AddBaseAuthRelationalUserStore()
    .AddDefaultUI();
```

`AddDefaultUI()` comes from `Microsoft.AspNetCore.Identity.UI`. Add Razor Pages and
map them in the host. Protect Identity's account management pages with the Identity
application cookie scheme. The [reference host](../samples/MachineService/Program.cs)
shows that policy and disables the public registration page.

After `UseAuthentication()` and `UseAuthorization()`, call
`MapBaseAuthTokenEndpoint()` and `MapBaseAuthLocalUserAuthorization()`. Protect user
API endpoints with `RequireAuthorization(BaseAuthPolicies.User)` and read the product
identity through `User.GetBaseAuthActor()`. The Identity cookie is used by the login
UI. API requests use an access token.

Use the default Identity store with `AddBaseAuthRelationalUserStore()` or register an
application-specific Identity store. Register a custom `ILocalUserAdmission` before
calling `AddBaseAuthLocalUsers()` to resolve the user's product ID and context from an
existing product store. The admission service has no HTTP or OpenIddict dependency.
Provision accounts through trusted application code and assign a context when the
product uses one. The reference host disables public registration. User access tokens
omit `context_id` when no context is assigned. A product with users who belong to
multiple tenants can check their tenant-specific roles using the user ID and the
tenant requested by the API call.

### Database migrations

`LocalIdentityDbContext` uses the standard Identity model with Base.Auth table names,
an optional application context, account status, and a unique normalized email index.
New accounts use UUIDv7 `Guid` keys. Both contexts use the same Basilisque provider
and connection settings. Each
provider assembly contains `Migrations/Auth` for `AuthDbContext` and
`Migrations/Identity` for `LocalIdentityDbContext`. Both contexts record
applied migration IDs in `__BASAuthMigrationsHistory`. Migration IDs must be unique
across the two Base.Auth contexts for each provider.

`AddBaseAuthRelationalPersistence()` registers the protocol initializer.
`AddBaseAuthRelationalUserStore()` adds the Identity initializer. A setup runner can
resolve `IEnumerable<IDbInitializer>` in a scope and apply each registered context
with `MigrateDatabase()`. The reference host uses this mechanism for
`--initialize-database`. When using a custom Identity store, its integration owns
the account schema and its migrations.

## Provision an account and a client

Run from the repository root. The reference host uses SQLite and a trusted development
HTTPS certificate by default. Set `BASE_AUTH_LOCAL_USER_PASSWORD` in the provisioning
process environment and remove it when the command completes.

```powershell
dotnet run --project samples/MachineService -- --initialize-database true --create-user reader@example.test
dotnet run --project samples/MachineService -- --register-user-client desktop-demo --redirect-uri http://127.0.0.1:8765/callback
dotnet run --project samples/MachineService
```

Pass `--context demo-installation` to `--create-user` to assign an application context.

Configure your OIDC client as a public client with authority `https://localhost:7443/`,
client ID `desktop-demo`, the registered redirect URI, response type `code`, S256 PKCE,
and scopes `openid api`. The client opens the system browser and handles the callback
at the registered redirect URI. Use the resulting access token to call
`https://localhost:7443/api/me`. The M2M sample client uses its separate client registration
and calls `/api/ping`.

The Identity account UI is under `/Identity/Account`. Users can change their password
and enroll an authenticator from the account management pages. Configure an Identity
email sender and account confirmation policy in a host that uses email verification
or self-service password recovery. The reference host uses administrative password
reset and does not deliver email.

```powershell
dotnet run --project samples/MachineService -- --reset-user-password reader@example.test
dotnet run --project samples/MachineService -- --disable-user reader@example.test
dotnet run --project samples/MachineService -- --enable-user reader@example.test
```

The password reset command also reads `BASE_AUTH_LOCAL_USER_PASSWORD`. Disabling an
account blocks sign-in and authorization code exchange. Already issued access tokens
remain valid until expiry. The local user flow uses short-lived access tokens and a
browser session for repeat authorization. It does not issue refresh tokens.

Use a persistent, protected ASP.NET Core Data Protection key ring in production.
Share it across instances of the same host so Identity cookies and recovery tokens
can be validated across restarts and instances.