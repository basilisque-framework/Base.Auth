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
# Basilisque - Base.Auth

## Overview

A modular authentication library based on OpenIddict. It provides machine-to-machine authentication using
Client Credentials and `private_key_jwt`, product service identities, and API token validation.
It also supports local user sign-in with ASP.NET Core Identity and Authorization Code with PKCE.

Base.Auth messages use English by default and include German satellite assemblies.
Set `CultureInfo.CurrentUICulture` in the host to select the language, or configure
request localization for an ASP.NET Core host. The MachineService sample supports
English and German requests. Framework messages and descriptions supplied by an
external authorization server follow their respective sources.

To override Base.Auth protocol error descriptions, register a closed
`IStringLocalizer<AuthProtocolMessages>` in the host, for example
`builder.Services.AddScoped<IStringLocalizer<AuthProtocolMessages>, PortalAuthLocalizer>()`.
Use the keys on `Basilisque.Base.Auth.Server.AuthProtocolMessages`. Return a
`LocalizedString` with `ResourceNotFound = true` for each key and culture that
should use the built-in resources. Protocol error codes stay stable across cultures.

[![NuGet Basilisque.Base.Auth.Client](https://img.shields.io/badge/NuGet_Basilisque.Base.Auth.Client-latest-%23004880.svg?logo=nuget)](https://www.nuget.org/packages/Basilisque.Base.Auth.Client)\
[![NuGet Basilisque.Base.Auth.Server](https://img.shields.io/badge/NuGet_Basilisque.Base.Auth.Server-latest-%23004880.svg?logo=nuget)](https://www.nuget.org/packages/Basilisque.Base.Auth.Server)\
[![NuGet Basilisque.Base.Auth.Abstractions](https://img.shields.io/badge/NuGet_Basilisque.Base.Auth.Abstractions-latest-%23004880.svg?logo=nuget)](https://www.nuget.org/packages/Basilisque.Base.Auth.Abstractions)\
[![NuGet Basilisque.Base.Auth.Client.Credentials.DataProtection](https://img.shields.io/badge/NuGet_Basilisque.Base.Auth.Client.Credentials.DataProtection-latest-%23004880.svg?logo=nuget)](https://www.nuget.org/packages/Basilisque.Base.Auth.Client.Credentials.DataProtection)\
[![License](https://img.shields.io/badge/License-Apache%20License%202.0-%23D22128.svg?logo=apache&logoColor=%23D22128)](LICENSE.txt)

## Reference applications

The [reference applications](samples/README.md) include an ASP.NET Core service
(`samples/MachineService`) and a console client for .NET and .NET Framework 4.8
(`samples/MachineClient`).

```powershell
dotnet run --project samples/MachineService -- --initialize-database true
```

The development service requires a trusted HTTPS development certificate.

## Building and testing

The [root solution](Basilisque.Base.Auth.slnx) contains the libraries, tests, samples, and
verification tools.

- `src/`: reusable libraries and their tests.
- `samples/`: executable reference applications that demonstrate library integration.
- `tools/`: compatibility verification programs.
- `docs/`: architecture and integration documentation.

Shared build settings, package versions, and the .NET tool manifest live at the repository root.
Build output is written to `artifacts/`. Use an SDK that supports the target framework
set by `BAS_Auth_TargetFramework` in [Directory.Build.props](Directory.Build.props).

```powershell
dotnet build Basilisque.Base.Auth.slnx
dotnet test --solution Basilisque.Base.Auth.slnx --results-directory artifacts/test-results
```

See the [testing guide](docs/testing.md) for solution filters, Visual Studio, and Windows
compatibility requirements.

## Documentation

The [documentation index](docs/README.md) links to the integration and architecture guides.
Start with the [M2M guide](docs/m2m.md) for credentials, database selection, and client
registration.
See [local user authentication](docs/local-users.md) for account provisioning and browser login.

## License

The Basilisque framework (including this repository) is licensed under the
[Apache License, Version 2.0](LICENSE.txt).
