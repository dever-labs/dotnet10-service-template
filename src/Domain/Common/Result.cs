namespace ServiceTemplate.Domain.Common;

/// <summary>
/// Represents the result of a domain operation — either a success value or a typed error.
/// Avoids throwing exceptions for expected failure paths.
/// </summary>
public readonly record struct Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public DomainError? Error { get; }
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value on a failed result.");

    private Result(T value) { _value = value; IsSuccess = true; }
    private Result(DomainError error) { Error = error; IsSuccess = false; }

#pragma warning disable CA1000 // Factory methods on the generic type are intentional for domain ergonomics
    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(DomainError error) => new(error);
#pragma warning restore CA1000

#pragma warning disable CA2225 // Implicit operators complement the named factory methods above
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(DomainError error) => Failure(error);
#pragma warning restore CA2225

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<DomainError, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(Error!.Value);
    }
}

/// <summary>Discriminates the category of a <see cref="DomainError"/> so callers can map to HTTP status codes without string-sniffing the Code.</summary>
public enum ErrorType { Validation, NotFound, Conflict, Unexpected }

/// <summary>Represents a domain error with a code, description, and type.</summary>
public readonly record struct DomainError(string Code, string Description, ErrorType Type)
{
    public static DomainError NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static DomainError Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static DomainError Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static DomainError Unexpected(string code, string description) => new(code, description, ErrorType.Unexpected);
}
