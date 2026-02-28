namespace ServiceTemplate.Domain.Common;

/// <summary>
/// Represents the result of a domain operation — either a success value or a typed error.
/// Avoids throwing exceptions for expected failure paths.
/// </summary>
public readonly record struct Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public Error? Error { get; }
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value on a failed result.");

    private Result(T value) { _value = value; IsSuccess = true; }
    private Result(Error error) { Error = error; IsSuccess = false; }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(Error!.Value);
}

/// <summary>Represents a domain error with a code and description.</summary>
public readonly record struct Error(string Code, string Description)
{
    public static Error NotFound(string code, string description) => new(code, description);
    public static Error Validation(string code, string description) => new(code, description);
    public static Error Conflict(string code, string description) => new(code, description);
    public static Error Unexpected(string code, string description) => new(code, description);
}
