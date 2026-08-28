namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Joueur connecté. Seul le nom est garanti : un serveur interrogé en RCON ne donne souvent que
// lui et un identifiant. GroupName couvre aussi bien une guilde qu'une tribu selon le jeu.
public sealed record GameServerLivePlayer(
    string Name,
    string? Id,
    int? Ping,
    int? Level,
    int? Health,
    int? MaxHealth,
    string? GroupId,
    string? GroupName,
    // Coordonnées lisibles, telles que le jeu les affiche à ses joueurs.
    double? MapX,
    double? MapY,
    // Coordonnées brutes du monde. Elles ne servent qu'à la projection sur une carte, faite par
    // le front : lui seul connaît les images et leurs bornes.
    double? PositionX,
    double? PositionY,
    GameServerLiveCompanion? Companion);
