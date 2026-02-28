---
applyTo: "src/Domain/**"
---

# Domain Layer Instructions

## Rules
- Zero external package dependencies (no NuGet packages allowed in Domain.csproj)
- All public methods that can fail must return `Result<T>` — never throw
- Business logic lives here, not in Application handlers
- Use `TimeProvider` (injected) for all date/time operations — never `DateTime.UtcNow`
- Domain events inherit from `IDomainEvent` and are raised via `AddDomainEvent(...)`

## Entity Pattern
```csharp
public sealed class MyEntity : Entity
{
    public string Name { get; private set; } = string.Empty;

    private MyEntity() { } // EF constructor

    public static Result<MyEntity> Create(string name, TimeProvider tp)
    {
        if (string.IsNullOrWhiteSpace(name)) return MyEntityErrors.NameRequired;
        var now = tp.GetUtcNow();
        return new MyEntity { Name = name.Trim(), CreatedAt = now, UpdatedAt = now };
    }
}
```

## Error Pattern
Every entity has a corresponding `<Entity>Errors` static class:
```csharp
public static class MyEntityErrors
{
    public static readonly Error NameRequired = Error.Validation("MyEntity.NameRequired", "Name is required.");
    public static readonly Error NotFound = Error.NotFound("MyEntity.NotFound", "Entity not found.");
}
```
