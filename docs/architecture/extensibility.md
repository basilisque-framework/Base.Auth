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

The optional `Basilisque.Base.Auth.Authorization.Abstractions` package defines
`IPermissionEvaluator`. Applications can provide their own implementation. The
`Basilisque.Base.Auth.Authorization` package provides the EF Core implementation
and the `IPermissionManagement` API. See [business permissions](../authorization.md)
for setup and administration.

A permission object type defines the supported action IDs. An object can have a parent
in the permission catalog. Roles group rules that allow or deny actions on these
objects. Assignments identify a user or service by actor kind and subject ID. Each
assignment has one application-defined scope ID and optional validity bounds. The start
is inclusive and the end is exclusive.

For each check, the application resolves all applicable scope IDs from trusted resource
data. For example, a room may have assignments that apply through its facility and
company. The evaluator considers assignments in any of those scopes. Actor context
does not imply a scope. An empty scope set matches no assignments.

The evaluator checks the target and its ancestors for rules with the requested action
ID. Any applicable denial overrides all grants. An unknown object, unsupported action,
or absence of a matching grant results in denial. `AreAllowed` evaluates a batch at one
instant and returns each decision under its caller-provided key.

## Native integration

A separate product adapter can expose the managed client to applications without a
managed .NET runtime. Define the ABI, calling convention, encoding, output ownership,
memory release, errors, and cancellation at that boundary. Verify Native AOT on the
actual process architecture and dependency chain. Keep any product-specific legacy
credential format in the adapter. The server can use the standard ASP.NET Core host.