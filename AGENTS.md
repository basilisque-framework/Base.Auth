# Repository Guidelines

## C# Code
- Use file-scoped namespaces (`namespace Foo.Bar;`).
- Prefer `using` declarations (`using var resource = ...;`) over `using` statements when their scope and disposal timing are appropriate.
- Add XML documentation comments to public and protected members.
- Use meaningful names consistent with the surrounding code.

## Tests
- Use TUnit for test definitions and lifecycle attributes.
- Use TUnit assertions by default; use Shouldly only when TUnit assertions cannot adequately express the assertion.
- Use NSubstitute for mocks and substitutes.
- Name tests `MethodUnderTest_ShouldDoSomething_InSpecificContext`.
- Structure each test using Arrange, Act, Assert (AAA), with one Act step and one behavior under test.
- Initialize per-test state in the test class constructor when possible. Use TUnit lifecycle methods when setup requires asynchronous work or lifecycle-specific behavior.
- Extract repeated setup and teardown logic into helper methods.