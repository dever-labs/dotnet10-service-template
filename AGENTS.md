# AGENTS.md — AI Coding Agent Guide

This file helps AI coding agents (GitHub Copilot coding agent, Claude, Gemini, etc.) work
effectively in this repository. Read this fully before making any changes.

> **Preferred model:** Claude (claude-3-7-sonnet or newer). Use Claude for Copilot Chat,
> agent-mode tasks, and the `@copilot` coding agent where model selection is available.

---

## Quick Start

```bash
# 1. Start infrastructure (Postgres, Seq, OTel Collector)
make infra-up

# 2. Apply database migrations
make migrate

# 3. Build the entire solution
make build

# 4. Run unit tests (fast, no Docker required)
make test

# 5. Run all tests (requires Docker)
make test-all
```

**The build MUST succeed with zero warnings and zero errors** before submitting any PR.
Run `make build` to verify. `TreatWarningsAsErrors=true` applies to all projects.

---

## Repository Map

```
src/Domain/                                 ← Core business logic, zero dependencies
  Common/Entity.cs                          ← Base entity with domain events
  Common/Result.cs                          ← Result<T> and Error types
  Todos/Todo.cs                             ← Todo aggregate root

src/Application/                            ← Use cases
  Common/Cqrs/                              ← Our custom ISender, IRequest, IRequestHandler
  Common/Behaviors/                         ← Logging + Validation pipeline behaviors
  Common/Interfaces/                        ← ITodoRepository, IUnitOfWork
  Todos/Commands/CreateTodo/                ← CreateTodoCommand + Handler + Validator
  Todos/Commands/UpdateTodo/
  Todos/Commands/DeleteTodo/
  Todos/Queries/GetTodo/
  Todos/Queries/GetTodos/
  Todos/TodoResponse.cs                     ← DTO for all Todo reads

src/Infrastructure/
  Persistence/AppDbContext.cs               ← EF Core context, also implements IUnitOfWork
  Persistence/Configurations/              ← IEntityTypeConfiguration<T> per entity
  Persistence/Repositories/               ← Concrete repository implementations
  AI/GitHubModelsClient.cs                 ← GitHub Models API client (OpenAI-compatible)

src/Api/
  Program.cs                               ← App composition root
  Endpoints/TodoEndpoints.cs               ← All Todo HTTP routes
  Middleware/GlobalExceptionHandler.cs     ← Maps exceptions to ProblemDetails

src/McpServer/                             ← MCP server for Copilot/AI agent tool use
  Tools/TodoTools.cs                       ← Exposes Todo API as MCP tools

tests/UnitTests/                           ← Fast, isolated, NSubstitute mocks
tests/IntegrationTests/                    ← Real PostgreSQL via Testcontainers
tests/AcceptanceTests/                     ← BDD-style HTTP API tests
```

---

## How to Add a New Feature

**Example: add a "Priority" field to Todo.**

### Step 1 — Domain (`src/Domain/Todos/Todo.cs`)
Add the property and update `Create`/`Update` methods. Return `Result<T>` on validation failure.

### Step 2 — Application Command (`src/Application/Todos/Commands/UpdateTodo/`)
Add `Priority` to `UpdateTodoCommand` record and `UpdateTodoCommandHandler`.

### Step 3 — Validator
Update `UpdateTodoCommandValidator` in FluentValidation style:
```csharp
RuleFor(x => x.Priority).InclusiveBetween(1, 5).When(x => x.Priority.HasValue);
```

### Step 4 — Infrastructure
- Update `TodoConfiguration.cs` to map the new column
- Run `make migration NAME=AddTodoPriority`

### Step 5 — API Endpoint
Update `src/Api/Endpoints/TodoEndpoints.cs` to pass the new field through.

### Step 6 — Tests
- Add a unit test in `tests/UnitTests/Todos/` (test the handler directly)
- Add an integration test asserting the HTTP round-trip

---

## Critical Patterns — Do Not Deviate

### Result<T> — Never throw for expected failures
```csharp
// ✅ Correct
public Result<Todo> Complete(TimeProvider tp) {
    if (Status == TodoStatus.Done) return TodoErrors.AlreadyCompleted;
    Status = TodoStatus.Done;
    return this;
}

// ❌ Wrong — never throw domain exceptions
public void Complete() {
    if (Status == TodoStatus.Done) throw new InvalidOperationException("Already done");
}
```

### CQRS — Handlers use HandleAsync
```csharp
// ✅ Our interface
public async Task<Result<TodoResponse>> HandleAsync(CreateTodoCommand request, CancellationToken ct)

// ❌ Wrong (MediatR style — MediatR is NOT in this project)
public async Task<Result<TodoResponse>> Handle(CreateTodoCommand request, CancellationToken ct)
```

### Minimal APIs — no controllers
```csharp
// ✅ Correct
app.MapPost("/api/todos", async (CreateTodoCommand cmd, ISender sender, CancellationToken ct) => ...);

// ❌ Wrong — no ControllerBase, no [ApiController]
```

---

## Validation Before Submitting

Run all of these and ensure they pass:

```bash
make lint         # dotnet format --verify-no-changes
make build        # zero warnings, zero errors
make test         # 7+ unit tests, all green
```

If you changed Infrastructure or added a migration, also run:
```bash
make migrate      # ensure migrations apply cleanly
```

---

## Key Libraries

| Library | Purpose | License |
|---------|---------|---------|
| FluentValidation 11 | Request validation in Application layer | MIT |
| EF Core 9 + Npgsql 9 | ORM + PostgreSQL | MIT / Apache-2 |
| Serilog | Structured logging | Apache-2 |
| OpenTelemetry | Traces + metrics via OTLP | Apache-2 |
| Testcontainers | Spin up PostgreSQL for tests | MIT |
| Respawn | Reset DB state between integration tests | MIT |
| xUnit + NSubstitute + FluentAssertions | Testing | Apache-2 / BSD / Apache-2 |

**MediatR is NOT used** (commercial license). All CQRS abstractions live in `Application.Common.Cqrs`.
