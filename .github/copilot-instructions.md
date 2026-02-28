# GitHub Copilot Instructions

## Project Architecture

This is a **.NET 10 service** using **Clean Architecture** with a custom CQRS dispatcher (no MediatR — it is now commercial).

```
src/Domain/          ← Entities, Result<T>, domain errors — zero external dependencies
src/Application/     ← CQRS commands/queries, validators, DTOs, ISender/IRequestHandler
src/Infrastructure/  ← EF Core + PostgreSQL, repository implementations
src/Api/             ← Minimal APIs, middleware, Program.cs
```

## Rules — Always Follow These

### Error Handling
- **Never throw exceptions** for expected domain failures. Use `Result<T>` from `Domain.Common`.
- Return `Error.Validation(...)`, `Error.NotFound(...)`, `Error.Conflict(...)` from domain methods.
- Map results to HTTP responses with `.Match(ok => ..., error => Results.Problem(...))` in endpoints.

### CQRS — Adding a New Feature
Every feature follows this exact pattern:

1. **Domain**: Add/modify entity in `src/Domain/<Aggregate>/`
2. **Application command**: `src/Application/<Aggregate>/Commands/<Action>/<Action>Command.cs` implementing `IRequest<Result<T>>`
3. **Handler**: `<Action>CommandHandler.cs` implementing `IRequestHandler<TRequest, TResponse>` — method is `HandleAsync`
4. **Validator**: `<Action>CommandValidator.cs` extending `AbstractValidator<TCommand>`
5. **Endpoint**: Add route to existing `src/Api/Endpoints/<Aggregate>Endpoints.cs` — use `sender.SendAsync(...)`
6. **EF config**: Update `src/Infrastructure/Persistence/Configurations/<Aggregate>Configuration.cs`
7. **Migration**: `make migration NAME=<DescriptiveName>`
8. **Tests**: Unit test the handler directly (no ISender needed), integration test via HTTP

### The ISender / Handler Contract
```csharp
// Dispatch via endpoint (never new up a handler directly in production code)
var result = await sender.SendAsync(new CreateTodoCommand(title, desc, due), ct);

// In unit tests, call handlers directly:
var result = await _handler.HandleAsync(command, CancellationToken.None);
```

### Testing Conventions
- **Unit tests**: instantiate the handler directly, inject NSubstitute mocks
- **Integration tests**: use `IntegrationTestFixture` (Testcontainers + Respawn), call via `HttpClient`
- **Acceptance tests**: BDD-style `[Fact(DisplayName = "Given... When... Then...")]`
- Always `await fixture.ResetDatabaseAsync()` in `InitializeAsync()` for integration tests

### Code Style
- Use **file-scoped namespaces** (`namespace Foo.Bar;`)
- Use **primary constructors** for DI injection
- Prefer **`sealed`** on concrete classes
- Use **`record`** for commands/queries/DTOs (immutable by default)
- `async` methods must end with `Async`

## What NOT to Do
- ❌ Don't add `using MediatR;` — we have our own CQRS in `Application.Common.Cqrs`
- ❌ Don't use `[ApiController]` or `ControllerBase` — this project uses Minimal APIs only
- ❌ Don't add new NuGet packages without checking the license first
- ❌ Don't use `DateTime` — use `DateTimeOffset` and inject `TimeProvider`
- ❌ Don't bypass the repository pattern by injecting `AppDbContext` outside Infrastructure

## Project Commands
```bash
make build              # Build (TreatWarningsAsErrors=true)
make test               # Unit tests only (fast, no Docker)
make test-integration   # Needs Docker running
make test-acceptance    # Needs Docker running
make migrate            # Apply EF Core migrations
make migration NAME=X   # Create new migration
make run                # Hot-reload API on http://localhost:5000
make infra-up           # Start Postgres + Seq + OTel Collector
```
