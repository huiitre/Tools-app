namespace Tools.Api.Modules.Core.AppLogs.Application;

// Événement applicatif à enregistrer. Les informations propres à la requête HTTP (IP et
// user-agent) sont ajoutées par AppLogService et ne circulent jamais dans la commande.
public sealed record AppLogCommand(
    long? ModuleId,
    string AreaCode,
    string ActionCode,
    long? UserId = null,
    object? Metadata = null);
