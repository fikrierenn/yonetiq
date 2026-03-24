namespace YonetIQ.Data.Infrastructure;

/// <summary>
/// Tüm servis metodlarından dönen standart sonuç nesnesidir.
/// </summary>
/// <typeparam name="T">Dönen verinin tipi</typeparam>
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }

    public static ServiceResult<T> Success(T data, string message = "") => new()
    {
        IsSuccess = true,
        Data = data,
        Message = message
    };

    public static ServiceResult<T> Failure(string message, string? errorCode = null) => new()
    {
        IsSuccess = false,
        Message = message,
        ErrorCode = errorCode
    };
}

public class ServiceResult : ServiceResult<object>
{
    public static ServiceResult Success(string message = "") => new()
    {
        IsSuccess = true,
        Message = message
    };

    public static new ServiceResult Failure(string message, string? errorCode = null) => new()
    {
        IsSuccess = false,
        Message = message,
        ErrorCode = errorCode
    };
}
