namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Demande d'action différée enregistrée auprès de IGameServerActionCountdownService. Les
// paramètres et le code d'action sont déjà validés par le use case au moment de la planification :
// le service de compte à rebours ne revalide rien, il exécute.
public sealed record ScheduledGameServerAction(
    GameServerTarget Target,
    string ActionCode,
    IReadOnlyDictionary<string, string> Parameters,
    int DelaySeconds);
