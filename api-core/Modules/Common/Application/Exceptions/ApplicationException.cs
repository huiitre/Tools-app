public class ApplicationException : Exception
{
    protected ApplicationException(ErrorKind kind, string code, string message)
        : base(message)
    {
        Kind = kind;
        Code = code;
    }

    public ErrorKind Kind { get; }

    public string Code { get; }

    public static ApplicationException Validation(string code, string message) =>
        new(ErrorKind.Validation, code, message);

    public static ApplicationException NotFound(string code, string message) =>
        new(ErrorKind.NotFound, code, message);

    public static ApplicationException Conflict(string code, string message) =>
        new(ErrorKind.Conflict, code, message);

    public static ApplicationException Unauthorized(string code, string message) =>
        new(ErrorKind.Unauthorized, code, message);

    public static ApplicationException Forbidden(string code, string message) =>
        new(ErrorKind.Forbidden, code, message);

    public static ApplicationException Unavailable(string code, string message) =>
        new(ErrorKind.Unavailable, code, message);
}
