namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Corps de POST /gameservers/{slug}/actions/{actionCode}. DelaySeconds est générique (voir
// GameServerActionDefinition.SupportsDelay), distinct des Parameters propres à chaque jeu.
public sealed record ExecuteGameServerActionRequest(
    Dictionary<string, string>? Parameters,
    int? DelaySeconds);
