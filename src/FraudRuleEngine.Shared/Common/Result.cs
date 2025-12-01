namespace FraudRuleEngine.Shared.Common;

public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; private set; }

    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(T value) : base(true, null)
    {
        Value = value;
    }

    private Result(string error) : base(false, error)
    {
    }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(string error) => new(error);
}

