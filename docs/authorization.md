# Business permissions

Reference the Base.Auth persistence package for SQLite, PostgreSQL, or SQL Server.
Register its services using the same Basilisque provider configuration as for the
[authentication stores](m2m.md#configure-persistence), then enable the permission store.

```csharp
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;

builder.Services.AddBaseAuthRelationalAuthorization();
```

This registers scoped `IPermissionEvaluator` and `IPermissionManagement` services.
`PermissionDbContext` uses the configured Basilisque provider and ships with its
own migrations for all three providers. Apply them through the same
`IDbInitializer` startup flow as the authentication stores. The permission tables
use the `Auth_Permission` prefix. Contexts in the same database share the
`__BASAuthMigrationsHistory` table. Keep migration IDs distinct by including
`Auth_`, `Identity_`, or `Permissions_` after the EF Core timestamp.
The connection can be overridden for `PermissionDbContext` through the Basilisque
connection configuration.

For design-time checks, run EF Core commands from `samples/MachineService` so
the generated factory can load the sample's `appsettings.json`. Set
`Database__Provider` to the selected provider and supply the corresponding
`Database__ConnectionStrings__<Provider>__ConnectionString` as needed. For example,
to check the SQLite snapshot:

```powershell
$env:Database__Provider = 'SQLite'
dotnet ef migrations has-pending-model-changes --project ../../src/DataAccess.EntityFramework.SQLite/Base.Auth.DataAccess.EntityFramework.SQLite.csproj --startup-project Base.Auth.MachineService.csproj --context PermissionDbContext
```

Use the matching provider project for PostgreSQL or SQL Server. The provider
project supplies the generated design-time factory.

## Manage permissions

Use `CreateObjectType` to register the actions available on objects of that type.
Use `SetObjectTypeActions` to replace that list when the application catalog
changes. The operation preserves action metadata and leaves actions available to
other types. Remove any explicit rules for the affected objects before detaching
an action they use.
Use `CreateObject` from application code when a business resource becomes available
for authorization. Allocate a new key for each new resource.
`MoveObject` changes inherited rules. `DeleteObject` removes
a leaf and its explicit rules. Move or delete its children before deleting a parent.

`SaveRole` creates a role or replaces all its explicit rules. Existing assignments
continue to apply. `AssignRole` returns an ID that callers can pass to
`RevokeAssignment`. Use `GetCatalog`, `GetRoles`, and `GetAssignments` to build
application-owned administration. The application authorizes these operations.
Objects, object types, actions, and roles can have optional display labels up to
256 characters. Use the key when a label is missing. Update or clear a label with
the corresponding `Set...DisplayName` method. `GetCatalog` includes the action
labels. `SaveRole` preserves the existing label when the supplied label is null.
Objects, types, and actions also expose an optional `SourceKey` and a `Hidden`
flag. The application uses them to identify entries it supplies and control
visibility in its administration UI. Both fields are metadata for the application.
Roles, rules, and assignments record Basilisque creation and modification stamps.
User stamps use the application's Basilisque `IUserContext<string>` when available.
Stored string IDs are limited to 128 characters. Use stable IDs that are distinct
under the database's collation and use their exact spelling in checks.

The stored catalog and roles use GUID primary keys. Business keys have unique
indexes and can be changed with `RenameObjectType`, `RenameObject`, `RenameAction`,
and `RenameRole`. Existing relationships continue to use the same GUIDs. After a
rename, use the new key in application calls. An action rename applies to every
type and rule that references that action. Actor subject and scope IDs remain
application-defined strings.

## Evaluate permissions

Build each `PermissionCheck` from the target object, action, and applicable scope IDs
resolved by the application. Call `IsAllowed` for one decision or `AreAllowed` with
a dictionary of caller-defined keys. The evaluator reads current assignments and
rules on each call. It loads object paths in groups by depth and uses one evaluation
instant for the batch.

Use `IPermissionDiagnostics.Explain` in administrative tools to return each
decision with its applicable rules, including the source role, the rule's scope
matches, and whether the rule was inherited from an ancestor. The EF diagnostics
come from the registered evaluator when it implements `IPermissionDiagnostics`.
Applications can also register a separate matching diagnostic implementation.
Resolving diagnostics requires one of these two implementations.

Assignment bounds are stored in UTC `ValidFromUtc` and `ValidUntilUtc` columns.
The provider uses `datetime2` on SQL Server, `timestamp with time zone` on
PostgreSQL, and date-time text on SQLite. The public API accepts
`DateTimeOffset?` bounds and returns them in UTC.

Applications with their own authorization can reference
`Basilisque.Base.Auth.Authorization.Abstractions` and provide their own
`IPermissionEvaluator`.