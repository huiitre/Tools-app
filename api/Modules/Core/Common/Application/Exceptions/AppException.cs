namespace Tools.Api.Modules.Core.Common.Application.Exceptions;

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

    // 400 — une valeur reçue est inexploitable : champ obligatoire vide, format ou borne non respectés.
    public static AppException Validation(string code, string message) =>
        new(ErrorKind.Validation, code, message);

    // 404 — la ressource désignée n'existe pas : identifiant inconnu, peer VPN demandé absent.
    public static AppException NotFound(string code, string message) =>
        new(ErrorKind.NotFound, code, message);

    // 409 — la demande contredit l'état actuel : nom déjà pris, doublon, transition impossible.
    public static AppException Conflict(string code, string message) =>
        new(ErrorKind.Conflict, code, message);

    // 401 — l'appelant n'est pas identifié : jeton absent, expiré ou illisible.
    public static AppException Unauthorized(string code, string message) =>
        new(ErrorKind.Unauthorized, code, message);

    // 403 — l'appelant est identifié mais n'a pas le droit : rôle insuffisant, module non accordé.
    public static AppException Forbidden(string code, string message) =>
        new(ErrorKind.Forbidden, code, message);

    // 503 — une dépendance nous fait défaut : service WireGuard injoignable, SMTP en panne.
    //       Seule nature journalisée en erreur : elle signale un incident, pas une faute d'usage.
    public static AppException Unavailable(string code, string message) =>
        new(ErrorKind.Unavailable, code, message);
}
