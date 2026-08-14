namespace Tools.ApiCore.Modules.Common.Application.Exceptions;

public class AppException : Exception
{
    protected AppException(ErrorKind kind, string code, string message)
        : base(message)
    {
        Kind = kind;
        Code = code;
    }

    public ErrorKind Kind { get; }

    public string Code { get; }

    public static AppException Validation(string code, string message) =>
        new(ErrorKind.Validation, code, message);

    public static AppException NotFound(string code, string message) =>
        new(ErrorKind.NotFound, code, message);

    public static AppException Conflict(string code, string message) =>
        new(ErrorKind.Conflict, code, message);

    public static AppException Unauthorized(string code, string message) =>
        new(ErrorKind.Unauthorized, code, message);

    public static AppException Forbidden(string code, string message) =>
        new(ErrorKind.Forbidden, code, message);

    public static AppException Unavailable(string code, string message) =>
        new(ErrorKind.Unavailable, code, message);
}
