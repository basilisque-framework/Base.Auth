using Basilisque.Base.Auth.Authorization.EntityFramework;
using Basilisque.Base.Auth.DataAccess.EntityFramework.Relational;
using Basilisque.Base.Auth.Identity;
using Basilisque.DataAccess.EntityFramework.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Basilisque.Base.Auth.Authorization.Integration.Tests;

/// <summary>Exercises permission management and evaluation against a relational database.</summary>
[Category("Integration")]
public sealed class PermissionPersistenceTests
{
    private const string Execute = "reports.execute";
    private readonly Actor _actor = new("anna", BaseAuthClaims.User);
    private readonly TimeProvider _clock = Substitute.For<TimeProvider>();
    private readonly DateTimeOffset _start = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Sets the clock used by the evaluator.</summary>
    public PermissionPersistenceTests()
    {
        _clock.GetUtcNow().Returns(_start);
    }

    /// <summary>Verifies that assignment changes are visible to an already resolved evaluator.</summary>
    [Test]
    public async Task RevokeAssignment_ShouldRemoveDenial_WhenAnotherScopeAllowsTheAction()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();
        await createCatalog(management);
        await management.SaveRole(new PermissionRole("reader", [new("reports", Execute, PermissionEffect.Allow)]));
        await management.SaveRole(new PermissionRole("restricted", [new("payroll", Execute, PermissionEffect.Deny)]));
        await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId, "company", _start, _start.AddMonths(3)));
        var assignmentId = await management.AssignRole(new RoleAssignment("restricted", _actor.Kind, _actor.SubjectId, "sunshine"));
        var checks = new Dictionary<string, PermissionCheck>
        {
            ["sunshine"] = new("payroll", Execute, ["company", "sunshine"]),
            ["lakeview"] = new("payroll", Execute, ["company", "lakeview"])
        };
        var before = await evaluator.AreAllowed(_actor, checks);

        // Act
        await management.RevokeAssignment(assignmentId);

        // Assert
        var after = await evaluator.AreAllowed(_actor, checks);
        await Assert.That(before["sunshine"]).IsFalse();
        await Assert.That(before["lakeview"]).IsTrue();
        await Assert.That(after["sunshine"]).IsTrue();
    }

    /// <summary>Verifies that role changes replace previous rules and preserve existing assignments.</summary>
    [Test]
    public async Task SaveRole_ShouldReplaceRules_WhenRoleAlreadyHasAssignments()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();
        await createCatalog(management);
        await management.SaveRole(new PermissionRole("reader", [new("payroll", Execute, PermissionEffect.Deny)]));
        var assignmentId = await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId, "company"));

        // Act
        await management.SaveRole(new PermissionRole("reader", [new("reports", Execute, PermissionEffect.Allow)]));

        // Assert
        var assignments = await management.GetAssignments(_actor.Kind, _actor.SubjectId);
        var check = new PermissionCheck("payroll", Execute, ["company"]);
        await Assert.That(assignments.ContainsKey(assignmentId)).IsTrue();
        await Assert.That(await evaluator.IsAllowed(_actor, check)).IsTrue();
    }

    /// <summary>Verifies that moving an object changes its inherited rules.</summary>
    [Test]
    public async Task MoveObject_ShouldChangeInheritedDecision_WhenParentChanges()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();
        await createCatalog(management);
        await management.SaveRole(new PermissionRole("reader", [new("reports", Execute, PermissionEffect.Allow)]));
        await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId, "company"));
        var check = new PermissionCheck("payroll", Execute, ["company"]);
        var before = await evaluator.IsAllowed(_actor, check);

        // Act
        await management.MoveObject("payroll", null);

        // Assert
        await Assert.That(before).IsTrue();
        await Assert.That(await evaluator.IsAllowed(_actor, check)).IsFalse();
    }

    /// <summary>Verifies that deleting a leaf removes the associated explicit rules.</summary>
    [Test]
    public async Task DeleteObject_ShouldRemoveRules_WhenObjectIsDeleted()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        await createCatalog(management);
        await management.SaveRole(new PermissionRole("reader", [new("payroll", Execute, PermissionEffect.Allow)]));

        // Act
        await management.DeleteObject("payroll");

        // Assert
        var catalog = await management.GetCatalog();
        var roles = await management.GetRoles();
        await Assert.That(catalog.Objects.Any(item => item.Id == "payroll")).IsFalse();
        await Assert.That(roles.Single().Rules.Count).IsEqualTo(0);
    }

    /// <summary>Verifies that deleting a role removes its stored assignments.</summary>
    [Test]
    public async Task DeleteRole_ShouldRemoveAssignments_WhenRoleIsDeleted()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        await management.SaveRole(new PermissionRole("reader", []));
        await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId, "company"));

        // Act
        await management.DeleteRole("reader");

        // Assert
        var assignments = await management.GetAssignments(_actor.Kind, _actor.SubjectId);
        await Assert.That(assignments.Count).IsEqualTo(0);
    }

    /// <summary>Verifies that changing a business key preserves the relationships to its record.</summary>
    [Test]
    [Arguments("type")]
    [Arguments("object")]
    [Arguments("action")]
    [Arguments("role")]
    public async Task Rename_ShouldPreserveEffectivePermissions_WhenBusinessKeyChanges(string kind)
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();
        await management.CreateObjectType(new PermissionObjectType("folder", [Execute]));
        await management.CreateObjectType(new PermissionObjectType("report", [Execute]));
        await management.CreateObject(new PermissionObject("reports", "folder"));
        await management.CreateObject(new PermissionObject("payroll", "report", "reports"));
        await management.SaveRole(new PermissionRole("reader", [new("reports", Execute, PermissionEffect.Allow)]));
        var assignmentId = await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId, "company"));
        const string NewAction = "reports.run";

        // Act
        await (kind switch
        {
            "type" => management.RenameObjectType("folder", "report-folder"),
            "object" => management.RenameObject("reports", "report-library"),
            "action" => management.RenameAction(Execute, NewAction),
            "role" => management.RenameRole("reader", "report-reader"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });

        // Assert
        var assignments = await management.GetAssignments(_actor.Kind, _actor.SubjectId);
        var roles = await management.GetRoles();
        var catalog = await management.GetCatalog();
        var actionId = kind == "action" ? NewAction : Execute;
        var folderId = kind == "object" ? "report-library" : "reports";
        await Assert.That(await evaluator.IsAllowed(_actor, new PermissionCheck("payroll", actionId, ["company"]))).IsTrue();
        await Assert.That(assignments[assignmentId].RoleId).IsEqualTo(kind == "role" ? "report-reader" : "reader");
        await Assert.That(roles.Single().Rules.Single().ObjectId).IsEqualTo(folderId);
        await Assert.That(roles.Single().Rules.Single().ActionId).IsEqualTo(actionId);
        await Assert.That(catalog.Objects.Single(item => item.Id == "payroll").ParentId).IsEqualTo(folderId);
        await Assert.That(catalog.Objects.Single(item => item.Id == folderId).TypeId)
            .IsEqualTo(kind == "type" ? "report-folder" : "folder");
        if (kind == "action")
            await Assert.That(await evaluator.IsAllowed(_actor, new PermissionCheck("payroll", Execute, ["company"]))).IsFalse();
    }

    /// <summary>Verifies that the database query and rule evaluation share one instant across the batch.</summary>
    [Test]
    public async Task AreAllowed_ShouldUseOneEvaluationTime_WhenAssignmentExpires()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();
        await createCatalog(management);
        await management.SaveRole(new PermissionRole("reader", [new("reports", Execute, PermissionEffect.Allow)]));
        var expires = _start.AddMonths(3);
        await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId, "company", _start, expires));
        _clock.GetUtcNow().Returns(expires.AddTicks(-1), expires);
        var check = new PermissionCheck("payroll", Execute, ["company"]);
        var checks = new Dictionary<string, PermissionCheck> { ["first"] = check, ["second"] = check };

        // Act
        var decisions = await evaluator.AreAllowed(_actor, checks);

        // Assert
        await Assert.That(decisions["first"] && decisions["second"]).IsTrue();
    }

    /// <summary>Verifies that display labels survive a round trip through the permission store.</summary>
    [Test]
    public async Task GetCatalog_ShouldIncludeDisplayNames_WhenCatalogLabelsAreConfigured()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        await management.CreateObjectType(new PermissionObjectType("report", [Execute], "Report"));
        await management.CreateObject(new PermissionObject("payroll", "report", displayName: "Payroll"));
        await management.SetActionDisplayName(Execute, "Run");

        // Act
        var catalog = await management.GetCatalog();

        // Assert
        await Assert.That(catalog.ObjectTypes.Single().DisplayName).IsEqualTo("Report");
        await Assert.That(catalog.Objects.Single().DisplayName).IsEqualTo("Payroll");
        await Assert.That(catalog.Actions.Single().DisplayName).IsEqualTo("Run");
    }

    /// <summary>Verifies that assignment bounds are returned as UTC instants.</summary>
    [Test]
    public async Task GetAssignments_ShouldReturnUtcBounds_WhenAssignedWithANonZeroOffset()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        await management.SaveRole(new PermissionRole("reader", [], "Report reader"));
        var localStart = _start.ToOffset(TimeSpan.FromHours(2));
        var id = await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId,
            "company", localStart, localStart.AddDays(1)));

        // Act
        var assignments = await management.GetAssignments(_actor.Kind, _actor.SubjectId);

        // Assert
        await Assert.That(assignments[id].ValidFrom!.Value.EqualsExact(_start)).IsTrue();
        await Assert.That(assignments[id].ValidUntil!.Value.EqualsExact(_start.AddDays(1))).IsTrue();
    }

    /// <summary>Verifies that a visibility hint does not alter evaluation and that matching rules can be explained.</summary>
    [Test]
    public async Task Explain_ShouldShowDirectAndInheritedRules_WhenHiddenObjectHasConflictingRules()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        var diagnostics = scope.ServiceProvider.GetRequiredService<IPermissionDiagnostics>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();
        await createCatalog(management);
        await management.SetObjectMetadata("payroll", "product.reports", hidden: true);
        await management.SaveRole(new PermissionRole("reader", [new("reports", Execute, PermissionEffect.Allow)]));
        await management.SaveRole(new PermissionRole("restricted", [new("payroll", Execute, PermissionEffect.Deny)]));
        await management.AssignRole(new RoleAssignment("reader", _actor.Kind, _actor.SubjectId, "company"));
        await management.AssignRole(new RoleAssignment("restricted", _actor.Kind, _actor.SubjectId, "sunshine"));
        var checks = new Dictionary<string, PermissionCheck>
        {
            ["sunshine"] = new("payroll", Execute, ["company", "sunshine"]),
            ["lakeview"] = new("payroll", Execute, ["company"])
        };

        // Act
        var decisions = await diagnostics.Explain(_actor, checks);

        // Assert
        var catalog = await management.GetCatalog();
        var payroll = catalog.Objects.Single(item => item.Id == "payroll");
        await Assert.That(payroll.SourceKey).IsEqualTo("product.reports");
        await Assert.That(payroll.Hidden).IsTrue();
        await Assert.That(decisions["sunshine"].Allowed).IsFalse();
        await Assert.That(decisions["lakeview"].Allowed).IsTrue();
        await Assert.That(await evaluator.IsAllowed(_actor, checks["lakeview"])).IsTrue();
        var matches = decisions["sunshine"].MatchingRules;
        var grant = matches.Single(item => item.Rule.Effect == PermissionEffect.Allow);
        var denial = matches.Single(item => item.Rule.Effect == PermissionEffect.Deny);
        await Assert.That(grant.Inherited && grant.RoleId == "reader" && grant.Rule.ObjectId == "reports"
            && grant.ScopeIds.Single() == "company").IsTrue();
        await Assert.That(!denial.Inherited && denial.RoleId == "restricted" && denial.ScopeIds.Single() == "sunshine").IsTrue();
    }

    /// <summary>Checks that a type update reuses catalog actions and preserves existing rules.</summary>
    [Test]
    public async Task SetObjectTypeActions_ShouldPreserveExistingRules_WhenAddingAnAction()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        await createCatalog(management);
        await management.SetActionDisplayName(Execute, "Run");
        await management.SaveRole(new PermissionRole("reader", [new("payroll", Execute, PermissionEffect.Allow)]));

        // Act
        await management.SetObjectTypeActions("folder", [Execute, "reports.edit"]);

        // Assert
        var catalog = await management.GetCatalog();
        await Assert.That(catalog.ObjectTypes.Single().ActionIds.Contains("reports.edit")).IsTrue();
        await Assert.That(catalog.Actions.Single(action => action.Id == Execute).DisplayName).IsEqualTo("Run");
        var roles = await management.GetRoles();
        await Assert.That(roles.Single().Rules.Single().ActionId).IsEqualTo(Execute);
    }

    /// <summary>Checks that an action with explicit rules cannot be detached from its type.</summary>
    [Test]
    public async Task SetObjectTypeActions_ShouldRejectRemoval_WhenAnExplicitRuleUsesTheAction()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        await createCatalog(management);
        await management.SaveRole(new PermissionRole("reader", [new("payroll", Execute, PermissionEffect.Deny)]));

        // Act
        Func<Task> update = () => management.SetObjectTypeActions("folder", []);

        // Assert
        await Assert.That(update).Throws<InvalidOperationException>();
        var catalog = await management.GetCatalog();
        await Assert.That(catalog.ObjectTypes.Single().ActionIds.Contains(Execute)).IsTrue();
    }

    /// <summary>Checks that detaching an unused action preserves it for other object types.</summary>
    [Test]
    public async Task SetObjectTypeActions_ShouldKeepSharedAction_WhenRemovingItFromOneType()
    {
        // Arrange
        using var database = new TestDatabase();
        await using var services = createServices(database.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        var management = scope.ServiceProvider.GetRequiredService<IPermissionManagement>();
        await createCatalog(management);
        await management.CreateObjectType(new PermissionObjectType("standalone-report", [Execute]));

        // Act
        await management.SetObjectTypeActions("folder", []);

        // Assert
        var catalog = await management.GetCatalog();
        await Assert.That(catalog.ObjectTypes.Single(type => type.Id == "folder").ActionIds.Count).IsEqualTo(0);
        await Assert.That(catalog.ObjectTypes.Single(type => type.Id == "standalone-report").ActionIds.Contains(Execute)).IsTrue();
    }

    /// <summary>Checks that the diagnostics registration follows a replaced evaluator.</summary>
    [Test]
    public async Task ResolveDiagnostics_ShouldRejectDefaultDiagnostics_WhenEvaluatorDoesNotSupportIt()
    {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddBaseAuthRelationalAuthorization();
        collection.AddScoped(_ => Substitute.For<IPermissionEvaluator>());
        using var services = collection.BuildServiceProvider();
        using var scope = services.CreateScope();

        // Act
        Action resolve = () => scope.ServiceProvider.GetRequiredService<IPermissionDiagnostics>();

        // Assert
        await Assert.That(resolve).Throws<InvalidOperationException>();
    }

    private ServiceProvider createServices(string connectionString)
    {
        var collection = new ServiceCollection().AddSingleton(_clock)
            .AddLogging()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "SQLite",
                ["Database:ConnectionStrings:SQLite:ConnectionString"] = connectionString
            }).Build());
        DataAccess.EntityFramework.SQLite.IServiceCollectionExtensions.RegisterServices(collection);
        collection.AddBaseAuthRelationalAuthorization();
        var services = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        try
        {
            using var scope = services.CreateScope();
            scope.ServiceProvider.GetServices<IDbInitializer>()
                .Single(initializer => initializer.DbContext is PermissionDbContext).MigrateDatabase();
            return services;
        }
        catch
        {
            services.Dispose();
            throw;
        }
    }

    private static async Task createCatalog(IPermissionManagement management)
    {
        await management.CreateObjectType(new PermissionObjectType("folder", [Execute]));
        await management.CreateObject(new PermissionObject("reports", "folder"));
        await management.CreateObject(new PermissionObject("payroll", "folder", "reports"));
    }

    private sealed class TestDatabase : IDisposable
    {
        private readonly string _path;

        public TestDatabase()
        {
            _path = Path.Combine(Path.GetTempPath(), "base-auth-permissions-" + Guid.NewGuid().ToString("N") + ".db");
        }

        public string ConnectionString => $"Data Source={_path};Pooling=False;Foreign Keys=True";

        public void Dispose() => File.Delete(_path);
    }
}
