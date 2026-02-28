using FluentValidation;
using ServiceTemplate.Application.Common.Cqrs;

namespace ServiceTemplate.Application.Common.Behaviors;

/// <summary>Runs FluentValidation validators for each request before the handler executes.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerFunc<TResponse> nextHandler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nextHandler);

        if (!validators.Any())
        {
            return await nextHandler(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await nextHandler(cancellationToken);
    }
}
