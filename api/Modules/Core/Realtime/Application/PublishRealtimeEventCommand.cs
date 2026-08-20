using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Realtime.Application;

// Un seul des critères de ciblage est renseigné. TargetUserIds sert aux appelants qui
// connaissent déjà leurs destinataires (ex : les joueurs d'une partie) — le Core ne peut pas
// les résoudre lui-même, il n'a aucune connaissance de ce périmètre métier.
public sealed record PublishRealtimeEventCommand(
    string EventType,
    object Payload,
    long? TargetUserId = null,
    RoleCode? TargetMinRole = null,
    long? TargetModuleId = null,
    IReadOnlyCollection<long>? TargetUserIds = null);
