---
mode: agent
description: Create a new EF Core database migration
tools: [codebase, terminal]
---

Create a migration for: **${input:migrationDescription}**

## Steps

### 1. Verify the model changes are in place
Check that:
- The entity in `src/Domain/` has the new property/change
- The `IEntityTypeConfiguration<T>` in `src/Infrastructure/Persistence/Configurations/` maps it correctly
- The `DbSet<T>` exists in `AppDbContext` if this is a new entity

### 2. Create the migration
```bash
make migration NAME=${input:migrationName}
```

This runs `dotnet ef migrations add` targeting the Infrastructure project.

### 3. Review the generated migration
Open `src/Infrastructure/Persistence/Migrations/<timestamp>_${input:migrationName}.cs`.
Verify:
- `Up()` contains the expected schema change
- `Down()` correctly reverses it
- No unintended table/column drops

### 4. Apply and verify
```bash
make migrate    # applies pending migrations
make build      # verify no compilation errors
make test       # run unit tests
```

### Common pitfalls
- New required (non-nullable) columns on existing tables need a default value or `nullable: true` initially
- Renamed properties generate a Drop + Add unless you use `.HasColumnName(...)` in the config
- Never edit an already-applied migration — create a new one instead
