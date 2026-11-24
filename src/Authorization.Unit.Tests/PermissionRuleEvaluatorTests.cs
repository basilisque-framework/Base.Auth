using Basilisque.Base.Auth.Abstractions.Identity;
using Basilisque.Base.Auth.Authorization.Abstractions;

namespace Basilisque.Base.Auth.Authorization.Unit.Tests;

/// <summary>Verifies permission inheritance and scoped role evaluation.</summary>
[Category("Unit")]
public sealed class PermissionRuleEvaluatorTests
{
    private const string Execute = "reports.execute";
    private const string Edit = "reports.edit";
    private readonly Actor _actor = new("anna", BaseAuthClaims.User, "unrelated-context");
    private readonly DateTimeOffset _start = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly PermissionRuleEvaluator _evaluator;

    /// <summary>Creates the report catalog and scoped assignments for each test.</summary>
    public PermissionRuleEvaluatorTests()
    {
        _evaluator = new PermissionRuleEvaluator(
            [new("folder", [Execute]), new("report", [Execute, Edit]), new("group", [])],
            [new("reports", "folder"), new("finance", "group", "reports"), new("payroll", "report", "finance")],
            [
                new("reader", [new("reports", Execute, PermissionEffect.Allow)]),
                new("restricted", [new("reports", Execute, PermissionEffect.Deny)]),
                new("editor", [new("payroll", Execute, PermissionEffect.Allow), new("payroll", Edit, PermissionEffect.Allow)])
            ],
            [
                new("reader", BaseAuthClaims.User, "anna", "company", _start, _start.AddMonths(3)),
                new("restricted", BaseAuthClaims.User, "anna", "sunshine"),
                new("editor", BaseAuthClaims.User, "anna", "sunshine"),
                new("reader", BaseAuthClaims.Service, "import", "company")
            ]);
    }

    /// <summary>Verifies that an inherited denial overrides a direct grant across scopes.</summary>
    [Test]
    public async Task Evaluate_ShouldPreferInheritedDeny_WhenMultipleScopesApply()
    {
        // Arrange
        var check = new PermissionCheck("payroll", Execute, ["company", "sunshine"]);

        // Act
        var allowed = evaluate(_actor, check);

        // Assert
        await Assert.That(allowed).IsFalse();
    }

    /// <summary>Verifies that a denial applies only to its action.</summary>
    [Test]
    public async Task Evaluate_ShouldAllowAnotherAction_WhenAncestorDeniesExecute()
    {
        // Arrange
        var check = new PermissionCheck("payroll", Edit, ["sunshine", "company"]);

        // Act
        var allowed = evaluate(_actor, check);

        // Assert
        await Assert.That(allowed).IsTrue();
    }

    /// <summary>Verifies the inclusive start and exclusive end of an assignment.</summary>
    [Test]
    [Arguments(-1, false)]
    [Arguments(0, true)]
    [Arguments(89, true)]
    [Arguments(90, false)]
    public async Task Evaluate_ShouldApplyValidityBounds_WhenAssignmentIsTimeLimited(int daysAfterStart, bool expected)
    {
        // Arrange
        var at = _start.AddDays(daysAfterStart);
        var check = new PermissionCheck("payroll", Execute, ["company"]);

        // Act
        var allowed = evaluate(_actor, check, at);

        // Assert
        await Assert.That(allowed).IsEqualTo(expected);
    }

    /// <summary>Verifies that decisions require a known object and a supported action.</summary>
    [Test]
    [Arguments("missing", Execute)]
    [Arguments("payroll", "missing")]
    [Arguments("reports", Edit)]
    public async Task Evaluate_ShouldDeny_WhenObjectOrSupportedActionIsMissing(string objectId, string actionId)
    {
        // Arrange
        var check = new PermissionCheck(objectId, actionId, ["company", "sunshine"]);

        // Act
        var allowed = evaluate(_actor, check);

        // Assert
        await Assert.That(allowed).IsFalse();
    }

    /// <summary>Verifies that an assignment matches both the actor kind and subject ID.</summary>
    [Test]
    [Arguments(BaseAuthClaims.Service, true)]
    [Arguments(BaseAuthClaims.User, false)]
    public async Task Evaluate_ShouldMatchActorKind_WhenSubjectIdsAreEqual(string actorKind, bool expected)
    {
        // Arrange
        var actor = new Actor("import", actorKind);
        var check = new PermissionCheck("payroll", Execute, ["company"]);

        // Act
        var allowed = evaluate(actor, check);

        // Assert
        await Assert.That(allowed).IsEqualTo(expected);
    }

    /// <summary>Verifies that the actor context provides no implicit scope.</summary>
    [Test]
    public async Task Evaluate_ShouldDeny_WhenOnlyActorContextMatchesAnAssignment()
    {
        // Arrange
        var actor = new Actor("anna", BaseAuthClaims.User, "company");
        var check = new PermissionCheck("payroll", Execute, []);

        // Act
        var allowed = evaluate(actor, check);

        // Assert
        await Assert.That(allowed).IsFalse();
    }

    /// <summary>Verifies keyed results for the same object in different scopes.</summary>
    [Test]
    public async Task Evaluate_ShouldReturnDecisionsByKey_WhenSameActionHasDifferentScopes()
    {
        // Arrange
        var checks = new Dictionary<string, PermissionCheck>
        {
            ["payroll:sunshine"] = new("payroll", Execute, ["company", "sunshine"]),
            ["payroll:lakeview"] = new("payroll", Execute, ["company", "lakeview"])
        };

        // Act
        var decisions = _evaluator.Evaluate(_actor, checks, _start);

        // Assert
        await Assert.That(decisions["payroll:sunshine"]).IsFalse();
        await Assert.That(decisions["payroll:lakeview"]).IsTrue();
    }

    /// <summary>Verifies that the evaluator rejects cycles in the catalog.</summary>
    [Test]
    public async Task Constructor_ShouldRejectCatalog_WhenParentLinksFormCycle()
    {
        // Arrange
        PermissionObject[] objects = [new("first", "folder", "second"), new("second", "folder", "first")];

        // Act
        void create() => new PermissionRuleEvaluator([new("folder", [Execute])], objects, [], []);

        // Assert
        await Assert.That(create).Throws<ArgumentException>();
    }

    private bool evaluate(Actor actor, PermissionCheck check, DateTimeOffset? at = null)
        => _evaluator.Evaluate(actor, new Dictionary<string, PermissionCheck> { ["check"] = check }, at ?? _start)["check"];
}