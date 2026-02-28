---
mode: agent
description: Write or regenerate tests for a handler, endpoint, or domain method
tools: [codebase, terminal]
---

Write tests for: **${input:targetFile}**

## Determine the test type needed

Read the target file first. Then:

- If it's a command/query **handler** → write a **unit test** in `tests/UnitTests/`
- If it's an **endpoint** or requires a real database → write an **integration test** in `tests/IntegrationTests/`
- If it's a **domain entity method** → write a **unit test** testing the method directly

## Unit test rules
- Instantiate the handler directly (never go through `ISender`)
- Mock dependencies with `NSubstitute`: `Substitute.For<IRepository>()`
- Call `await _sut.HandleAsync(command, CancellationToken.None)`
- Use `FluentAssertions`: `result.IsSuccess.Should().BeTrue()`
- Test both happy path AND each failure case (every `Error.*` the code can return)

## Integration test rules
- Class must be in `[Collection(nameof(IntegrationTestCollection))]`
- Constructor takes `IntegrationTestFixture fixture`
- Implement `IAsyncLifetime`, call `await fixture.ResetDatabaseAsync()` in `InitializeAsync`
- Use `fixture.Client` (typed `HttpClient`) for all requests
- Use `Bogus` `Faker` for test data where appropriate

## Acceptance test rules
- Method name must use BDD display name: `[Fact(DisplayName = "Given X, when Y, then Z")]`
- Structure each test as Given / When / Then with comments

## Coverage target
Ensure every branch that returns an `Error` value has a corresponding test case.
Run `make test` after and confirm all tests pass.
