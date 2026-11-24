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
# Reference applications

These applications demonstrate how a product integrates Base.Auth. They are not NuGet packages
and are not required as additional services when the libraries are embedded in a product.

- `MachineService`: ASP.NET Core Web API with an OpenIddict token endpoint, a protected API endpoint,
  database configuration, and client administration commands. Its Dockerfile demonstrates deployment.
- `MachineClient`: .NET and .NET Framework 4.8 console client that obtains a token through Base.Auth.Client
  and uses its own HTTP client to call the API.

Run commands from the repository root. See [the M2M guide](../docs/m2m.md) for setup and credentials.

```powershell
dotnet run --project samples/MachineService -f net10.0 -- --initialize-database true
```

The examples reference the libraries under `src/` directly. Products can consume the corresponding
NuGet packages.