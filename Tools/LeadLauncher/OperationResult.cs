namespace LeadLauncher;

public sealed record OperationResult<T>(
    bool IsSuccess,
    T? Value,
    string ErrorCode,
    string Details)
{
    public static OperationResult<T> Success(T value) =>
        new(true, value, string.Empty, string.Empty);

    public static OperationResult<T> Failure(string errorCode, string details) =>
        new(false, default, errorCode, details);
}
