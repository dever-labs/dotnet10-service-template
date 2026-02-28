using FluentValidation;

namespace ServiceTemplate.Application.Todos.Commands.CreateTodo;

public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTimeOffset.UtcNow).When(x => x.DueDate.HasValue)
            .WithMessage("Due date must be in the future.");
    }
}
