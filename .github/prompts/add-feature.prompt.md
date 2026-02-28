---
mode: agent
description: Add a complete new feature end-to-end following Clean Architecture
tools: [codebase, terminal, githubRepo]
---

Add a new feature following Clean Architecture. The aggregate name is: **${input:aggregateName}**

## Steps to follow

### 1. Domain layer (`src/Domain/${input:aggregateName}/`)
Create:
- `${input:aggregateName}.cs` — aggregate root extending `Entity`. Private setters, `Create()` factory returning `Result<${input:aggregateName}>`.
- `${input:aggregateName}Errors.cs` — static class with typed `Error` fields using `Error.NotFound`, `Error.Validation`, `Error.Conflict`.
- `${input:aggregateName}CreatedEvent.cs` — domain event implementing `IDomainEvent`.

### 2. Application layer
Create the full CRUD set in `src/Application/${input:aggregateName}/`:
- `${input:aggregateName}Response.cs` — immutable record DTO with `From${input:aggregateName}()` factory
- `Commands/Create${input:aggregateName}/` — command record + handler + FluentValidation validator
- `Commands/Update${input:aggregateName}/` — command record + handler
- `Commands/Delete${input:aggregateName}/` — command record + handler
- `Queries/Get${input:aggregateName}/` — query record + handler
- `Queries/Get${input:aggregateName}s/` — query record + handler returning `PagedResult<${input:aggregateName}Response>`

Add repository interface to `src/Application/Common/Interfaces/I${input:aggregateName}Repository.cs`.

### 3. Infrastructure layer
- `src/Infrastructure/Persistence/Configurations/${input:aggregateName}Configuration.cs` — `IEntityTypeConfiguration<${input:aggregateName}>`
- `src/Infrastructure/Persistence/Repositories/${input:aggregateName}Repository.cs` — implement `I${input:aggregateName}Repository`
- Register in `src/Infrastructure/DependencyInjection.cs`
- Add `DbSet<${input:aggregateName}>` to `AppDbContext.cs`

### 4. API endpoint
Add to a new file `src/Api/Endpoints/${input:aggregateName}Endpoints.cs` and register in `Program.cs`.
All routes return `Result<T>` mapped via `.Match(...)`.

### 5. Migration
Run: `make migration NAME=Add${input:aggregateName}`

### 6. Tests
- Unit tests in `tests/UnitTests/${input:aggregateName}/`
- Integration tests in `tests/IntegrationTests/${input:aggregateName}/`

### Rules
- Never throw for expected failures — use `Result<T>`
- All handlers implement `HandleAsync` (not `Handle`)
- Dispatch via `sender.SendAsync(...)` in endpoints
- Use `DateTimeOffset`, inject `TimeProvider`
