namespace BillMitra.Contracts.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];
    public int StatusCode { get; set; } = 200;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new()
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200
        };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null, int statusCode = 400) =>
        new()
        {
            Success = false,
            Data = default,
            Message = message,
            Errors = errors ?? [],
            StatusCode = statusCode
        };
}
