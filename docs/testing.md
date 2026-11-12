# Testing

Tests use TUnit and Microsoft.Testing.Platform on the .NET target framework set in
[`Directory.Build.props`](../Directory.Build.props). The Windows compatibility suite
also starts processes under .NET Framework 4.8.

| Test project | Coverage |
|---|---|
| `Abstractions.Unit.Tests` | Actor contract |
| `Client.Unit.Tests` | Access token caching and concurrent requests |
| `Server.Unit.Tests` | Default service registration and actor mapping overrides |
| `Client.Credentials.DataProtection.Integration.Tests` | Private-key package protection with Data Protection |
| `Server.Integration.Tests` | OpenIddict protocol, ASP.NET Core pipeline, SQLite, key rotation, and replay protection |
| `DataAccess.EntityFramework.Relational.Integration.Tests` | Provider registration, EF models, migration assembly selection, migration SQL, and SQLite migrations |
| `DataAccess.EntityFramework.SqlServer.Integration.Tests` | SQL Server provider registration |
| `Client.Integration.Tests` | HTTPS client flows and protected key-package exchange under .NET Framework 4.8 and .NET |

The persistence suite runs migrations against SQLite. SQL Server and PostgreSQL coverage
is limited to provider configuration, models, and generated SQL.

## Command line

Run from the repository root.

```powershell
# All tests
dotnet test --solution Basilisque.Base.Auth.slnx -c Release --results-directory artifacts/test-results

# Unit tests
dotnet test --solution Basilisque.Base.Auth.Unit.Tests.slnf -c Release --results-directory artifacts/test-results/unit

# Integration tests
dotnet test --solution Basilisque.Base.Auth.Integration.Tests.slnf -c Release --results-directory artifacts/test-results/integration

# Windows compatibility tests
dotnet test --project src/Client.Integration.Tests -c Release
```

The solution filters select test projects and build their dependencies. For narrower
selection within a project, use a TUnit tree filter such as
`--treenode-filter "/*/*/*/*[Category=Integration]"`. Microsoft.Testing.Platform reports
exit code 8 when a selected test module finds no matching tests.

## Visual Studio

Enable Microsoft.Testing.Platform support in Visual Studio. Open the full solution and
filter Test Explorer by project or category, or open a unit or integration solution
filter to load only those test projects and their dependencies.

## Windows compatibility

The `WindowsCompatibility` cases require Windows, .NET Framework 4.8, and a .NET SDK that
supports the configured target framework.
The HTTPS cases require an existing, trusted ASP.NET Core development certificate and use
normal TLS validation. The key-package cases do not require HTTPS.

Each case uses its own temporary files under `artifacts/client-compatibility`. The fixture
chooses ports dynamically, checks readiness through discovery, and applies process
timeouts. It cleans up its child processes, databases, credentials, and Data Protection
key rings after each case.
