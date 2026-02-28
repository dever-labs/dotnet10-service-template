---
applyTo: "tests/**"
---

# Test Instructions

## Test Categories & What They Test

| Project | Tests | Uses |
|---------|-------|------|
| `UnitTests` | Command/query handlers in isolation | NSubstitute mocks, no I/O |
| `IntegrationTests` | Full HTTP round-trip with real DB | Testcontainers, Respawn |
| `AcceptanceTests` | User-facing behaviour, BDD style | Testcontainers |

## Unit Test Pattern
```csharp
public sealed class CreateXCommandHandlerTests
{
    private readonly IXRepository _repo = Substitute.For<IXRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly CreateXCommandHandler _sut;

    public CreateXCommandHandlerTests()
    {
        _time.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _sut = new CreateXCommandHandler(_repo, _uow, _time);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var result = await _sut.HandleAsync(new CreateXCommand("valid"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }
}
```

## Integration Test Pattern
```csharp
[Collection(nameof(IntegrationTestCollection))]
public sealed class XIntegrationTests(IntegrationTestFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync(); // ALWAYS reset DB
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateX_ValidRequest_Returns201()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/x", new { Name = "test" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

## Acceptance Test Pattern (BDD style)
```csharp
[Fact(DisplayName = "Given valid data, when X is created, then it should be retrievable")]
public async Task CreateAndRetrieve()
{
    // Given
    // When
    // Then
}
```

## Do NOT
- ❌ Don't use `Thread.Sleep` — use `Task.Delay` or avoid delays entirely
- ❌ Don't share state between tests — always reset with `ResetDatabaseAsync()`
- ❌ Don't call handlers via `ISender` in unit tests — call `HandleAsync` directly
- ❌ Don't assert on exact error messages — assert on `result.IsSuccess` and error codes
