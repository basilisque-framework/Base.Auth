# Extensibility design

Base.Auth is directly usable with its default components. Applications can configure it
directly, use a shared integration package, or replace individual components. The
[M2M guide](../m2m.md) lists the extension points available for technical clients and APIs.
This design also covers the user and authorization capabilities in the
[target architecture](target-architecture.md).

## Composition and configuration

| Extension | Examples |
|---|---|
| Configure | Token lifetime, API audience, and authority |
| Add | Permission definitions and explicitly configured external providers |
| Replace | Client registry, service identity mapping, or product authorization stores |
| Decorate | Auditing or caching that preserves the underlying contract |

Use the library's defaults where the application supplies no replacement. An invalid
explicit replacement should fail startup or the affected operation. Authentication and
authorization failures must never fall back silently to another source.

Compose configuration in a documented order, from library defaults through integration
conventions, product choices, and deployment settings. Validate the final provider and
options combination. Keep the underlying OpenIddict, Identity, and ASP.NET Core
configuration available where the integration supports it.

## Persistence boundaries

| Store | Data |
|---|---|
| OpenIddict | Clients, protocol authorizations, scopes, and token records |
| ASP.NET Core Identity | Local accounts, password hashes, sign-in links, and MFA state |
| Base.Auth or product | Internal profiles, customer memberships, roles, grants, and group mappings |

The stores can share a database or use different persistence implementations. Products
can keep the default protocol store and supply their own profile or authorization data.
Store contracts should express the guarantees needed by each operation, including
tenant isolation, uniqueness, atomic first-time provisioning, concurrent updates, and
auditability. When an operation spans providers, define its consistency and recovery
behavior explicitly.

## Permission catalogs and roles

A permission catalog defines the known product actions and their scope. Role
administration groups those permissions for a customer. Assignments connect roles to
users or supported external groups. Authorization evaluates the effective permissions
against the requested operation and resource.

The design provides a default catalog and supports product-defined permission points.
Applications can supply their own catalog or role store independently where their
contracts are compatible. Use stable, unique permission IDs. Treat unknown or removed
IDs as granting no access. If an application replaces the catalog, any exposed
administrative action still requires an explicit permission mapping.

## Native integration

A separate product adapter can expose the managed client to applications without a
managed .NET runtime. Define the ABI, calling convention, encoding, output ownership,
memory release, errors, and cancellation at that boundary. Verify Native AOT on the
actual process architecture and dependency chain. Keep any product-specific legacy
credential format in the adapter. The server can use the standard ASP.NET Core host.
