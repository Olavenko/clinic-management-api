namespace ClinicManagementAPI.Core.Models;

// Result<T> is a generic class that represents the result of an operation.
// It is used to return a value and a status code.
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    // Private constructor to prevent direct instantiation
    private Result(bool isSuccess, T? value, string? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    // Static factory methods to create instances of Result<T>
    public static Result<T> Success(T value) => new(true, value, null, 200);

    // Failure method with default status code of 400
    public static Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);
}