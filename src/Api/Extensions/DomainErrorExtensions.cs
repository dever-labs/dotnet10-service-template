using ServiceTemplate.Domain.Common;

namespace ServiceTemplate.Api.Extensions;

/// <summary>Maps a <see cref="DomainError"/> to the appropriate HTTP <see cref="IResult"/> using its <see cref="ErrorType"/>.</summary>
internal static class DomainErrorExtensions
{
    public static IResult ToProblemResult(this DomainError error) => error.Type switch
    {
        ErrorType.NotFound   => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status404NotFound),
        ErrorType.Validation => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status400BadRequest),
        ErrorType.Conflict   => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status409Conflict),
        _                    => Results.Problem(detail: error.Description, statusCode: StatusCodes.Status500InternalServerError),
    };
}
