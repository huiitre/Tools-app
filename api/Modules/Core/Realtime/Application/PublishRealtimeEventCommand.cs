using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Realtime.Application;

// Un seul des critères de ciblage est renseigné. TargetUserIds sert aux appelants qui
// connaissent déjà leurs destinataires (ex : les joueurs d'une partie) — le Core ne peut pas
// les résoudre lui-même, il n'a aucune connaissance de ce périmètre métier.
//
// Payload est nullable : la plupart des events de rafraîchissement (ex : "ton rôle a changé")
// n'ont rien à transporter, le front n'a qu'à rappeler la route REST qui fait foi.
public sealed record PublishRealtimeEventCommand(
    string EventType,
    object? Payload,
    long? TargetUserId = null,
    RoleCode? TargetMinRole = null,
    long? TargetModuleId = null,
    IReadOnlyCollection<long>? TargetUserIds = null)
{
    public static PublishRealtimeEventCommand ForUser(long userId, string eventType, object? payload = null) =>
        new(eventType, payload, TargetUserId: userId);

    public static PublishRealtimeEventCommand ForUsers(IReadOnlyCollection<long> userIds, string eventType, object? payload = null) =>
        new(eventType, payload, TargetUserIds: userIds);

    // Destinataires : tous les comptes dont le rôle global atteint au moins `minRole`.
    public static PublishRealtimeEventCommand ForMinRole(RoleCode minRole, string eventType, object? payload = null) =>
        new(eventType, payload, TargetMinRole: minRole);

    // Destinataires : tous les membres du module désigné.
    public static PublishRealtimeEventCommand ForModule(long moduleId, string eventType, object? payload = null) =>
        new(eventType, payload, TargetModuleId: moduleId);
}
