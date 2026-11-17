# Target architecture

This design covers both technical clients and future user sign-in. See the [M2M guide](../m2m.md)
for the supported integration and the [extensibility design](extensibility.md) for
component boundaries.

## Responsibilities

| Component | Responsibility |
|---|---|
| OpenIddict | OAuth 2.0 and OpenID Connect protocol handling, token issuance, and validation |
| ASP.NET Core Identity | Local user accounts, credentials, account recovery, and local sign-in |
| Base.Auth | Integration defaults, identity normalization, product admission, and authorization services |
| Optional integration package | Shared conventions, branding, configuration, custom stores, and legacy adapters |
| Product | Permission definitions and server-side checks on business operations and resources |
| Deployment | Issuers, audiences, credentials, keys, isolation boundaries, and enabled integrations |

Base.Auth provides default implementations for the capabilities an application enables.
Products can deploy independently or share an issuer. Shared sign-in still requires
product-specific admission, client registrations, API audiences, and permissions.

The design accommodates small on-premises installations, larger on-premises deployments,
and cloud hosting. A small installation can host the issuer and API together. Larger
deployments can separate and replicate components. Capacity planning uses concurrent
activity, sign-in peaks, latency, and availability targets.

## Issuer profiles

- **Integrated issuer.** The product's OpenIddict server issues its tokens. User sign-in can
  use local Identity accounts, an upstream OIDC provider such as Entra ID or Keycloak, or
  an AD integration. The issuer validates the upstream sign-in and links it to a local
  identity before issuing product tokens. Configure upstream providers at the issuer.
  Users' browsers still need to reach the sign-in pages involved in the flow.
- **Direct external issuer.** Clients acquire tokens from a configured external issuer.
  The API validates those tokens and maps their claims to the internal identity model.
  The mapping is specific to the external issuer and its token contract.

Configure trusted issuers, audiences, and product or customer assignments for each
deployment. A token claim or request header cannot establish a new trust relationship.
Deployments can distribute public settings such as authority, client ID, and API addresses
through a trusted configuration source. The issuer still enforces registered redirect URIs,
and each token recipient validates its trusted issuer.

## Identity and authorization

The authenticated actor represents a user or a service. Users have stable internal IDs for
profiles, preferences, and business data. Services have their own registered identities
and can be assigned an application context.

At the API boundary, validate the token and resolve the actor and permitted context.
Check product admission before evaluating permissions against the requested resource.
Business code uses that context consistently across HTTP endpoints and SignalR operations.

Link OIDC users by their validated issuer and subject, or use an explicitly supported
provider-specific key such as an Entra tenant ID and object ID. Account linking requires
stable identifiers. Pairwise subjects can differ across clients. Use an invitation,
administrative approval, or a configured group rule before creating a user profile.
Make first-time provisioning atomic and idempotent. Preserve internal IDs when migrating
users to a new identity provider.

Customers compose roles from product-defined permissions. A user can hold multiple
roles within a customer and product context. OAuth scopes govern protocol-level access.
The application evaluates business permissions server-side and defines when grants,
role changes, and revoked assignments take effect.

## Active Directory and MFA

Keep AD sign-in and group resolution at the authentication boundary. An AD integration
can use integrated Windows authentication or a separately supported sign-in method.
Where password entry is required, collect it at the sign-in host and apply the
directory's transport and credential policies.

Group admission rules and role mappings use stable identifiers with their directory
origin, such as AD SIDs. Resolve effective membership across the supported group
nesting, group scopes, domains, and forests. Test trust direction, selective
authentication, directory permissions, and network reachability for each supported
topology. Provider group claims can require a supplemental directory lookup.

Track the source of each group-derived role grant. Removing a group grant preserves
independent manual grants. Define refresh, revocation, and provider failure behavior.
Admission requiring a group depends on verified membership.

Intranet Kerberos SSO depends on browser or client configuration, device environment,
and access to the relevant infrastructure. OIDC session reuse in a system browser
depends on the provider session and browser context, including on mobile devices.

Require appropriate sign-in strength and freshness for each user operation. Local
sign-in can enforce local MFA. Federated sign-in interprets validated `acr`, `amr`,
and `auth_time` through provider-specific policies. Define enrollment, recovery, and
administrative reset procedures. Clients must support renewed sign-in or step-up when
an operation requires it. Service authentication uses separately provisioned and
rotated machine credentials.

## Client models

| Client | Model |
|---|---|
| Confidential service | Client Credentials, dedicated service identity, and short-lived access tokens |
| WinForms or native application | Authorization Code with PKCE, system browser, and a platform-appropriate callback |
| Angular or browser PWA | Prefer a backend for frontend with server-side tokens, protected cookies, and CSRF protection |
| Packaged Android or iOS application | Choose the native OAuth or browser model for the actual container and use the appropriate callback |
| Legacy native application | Product adapter with a small native ABI around the managed client |

Define how token expiry, refresh, logout, account suspension, and revocation affect each
session. SignalR connections also need explicit rules for expiry, reconnects, and
authorization changes during an active connection. Check business permissions at the
hub operation.

## Deployment boundaries

Use HTTPS, explicit issuer and audience configuration, narrowly registered browser
origins and redirects, and protected deployment credentials. Provision administrative
access explicitly and audit security-sensitive changes. Keep passwords, private keys,
and full tokens out of logs.

For clustered deployments, coordinate signing credentials, persistence, Data Protection
key rings, and sessions as required by the enabled features. See the
[M2M persistence setup](../m2m.md#configure-persistence) for the supported database providers.

### On-premises gateway deployments

Keep gateway admission, product identity, and end-to-end transport protection separate.
A deployment can connect outward from the customer's installation and expose browser
sign-in through an approved public HTTPS route. The browser must be able to navigate to
the issuer and any upstream sign-in pages. The chosen transport must preserve the
deployment's confidentiality requirements and support certificate lifecycle management.

A transport adapter can pass decrypted application requests into the normal product API
pipeline. If the gateway can route to other internal services, restrict the destinations
and operations explicitly. Keep the origin, distribution, and update trust for browser
PWA code as an independent design concern.

## References

- [OAuth 2.0 Security Best Current Practice](https://www.rfc-editor.org/rfc/rfc9700)
- [OAuth 2.0 for Native Apps](https://www.rfc-editor.org/rfc/rfc8252)
- [OAuth 2.0 Step Up Authentication Challenge Protocol](https://www.rfc-editor.org/rfc/rfc9470)
- [ASP.NET Core OIDC web authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication)
- [ASP.NET Core SignalR authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz)
- [Windows Authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth)
- [Active Directory security groups](https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/understand-security-groups)
- [Entra group claims](https://learn.microsoft.com/en-us/entra/identity/hybrid/connect/how-to-connect-fed-group-claims)
