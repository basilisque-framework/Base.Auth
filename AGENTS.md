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
# Repository Guidelines

## Language
- Use English throughout the repository, including code, identifiers, comments, documentation, scripts, and emitted messages.
- Runtime messages may use other languages as part of localization. Use English for messages that are not localized.
- Preserve proper names and legal notices.

## C# Code
- Use file-scoped namespaces (`namespace Foo.Bar;`).
- Use explicit constructors with a constructor body. Do not use primary constructors, including positional record declarations; declare record properties explicitly.
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