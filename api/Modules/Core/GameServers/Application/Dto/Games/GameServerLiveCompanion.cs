namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Créature accompagnant un joueur (pal, monture, familier selon le jeu).
public sealed record GameServerLiveCompanion(
    string Name,
    int? Level,
    int? Health,
    int? MaxHealth);
