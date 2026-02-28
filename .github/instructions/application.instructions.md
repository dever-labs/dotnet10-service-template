---
applyTo: "src/Application/**"
---

# Application Layer Instructions

## CQRS Pattern — Use These Interfaces (NOT MediatR)

```csharp
// All in ServiceTemplate.Application.Common.Cqrs namespace
IRequest<TResponse>                    // marker for commands/queries
IRequestHandler<TRequest, TResponse>  // implement HandleAsync(request, ct)
ISender                                // inject in endpoints, call SendAsync(request, ct)
IPipelineBehavior<TReq, TRes>          // cross-cutting concerns
```

## Command Template
```csharp
// Command record
public sealed record CreateFooCommand(string Name) : IRequest<Result<FooResponse>>;

// Handler
public sealed class CreateFooCommandHandler(
    IFooRepository repo, IUnitOfWork uow, TimeProvider tp)
    : IRequestHandler<CreateFooCommand, Result<FooResponse>>
{
    public async Task<Result<FooResponse>> HandleAsync(CreateFooCommand request, CancellationToken ct = default)
    {
        var result = Foo.Create(request.Name, tp);
        if (!result.IsSuccess) return result.Error!.Value;
        await repo.AddAsync(result.Value, ct);
        await uow.SaveChangesAsync(ct);
        return FooResponse.FromFoo(result.Value);
    }
}

// Validator
public sealed class CreateFooCommandValidator : AbstractValidator<CreateFooCommand>
{
    public CreateFooCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
```

## Response DTO Pattern
```csharp
public sealed record FooResponse(Guid Id, string Name, ...)
{
    public static FooResponse FromFoo(Foo foo) => new(foo.Id, foo.Name, ...);
}
```
