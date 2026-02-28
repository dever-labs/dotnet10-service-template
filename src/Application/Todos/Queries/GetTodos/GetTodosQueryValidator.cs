using FluentValidation;

namespace ServiceTemplate.Application.Todos.Queries.GetTodos;

public sealed class GetTodosQueryValidator : AbstractValidator<GetTodosQuery>
{
    public GetTodosQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
