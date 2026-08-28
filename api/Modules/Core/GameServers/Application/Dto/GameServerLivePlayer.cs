namespace Tools.Api.Modules.Core.GameServers.Application.Dto;

// Joueur connecté. Seul le nom est garanti : un serveur interrogé en RCON ne donne souvent que
// lui et un identifiant. GroupName couvre aussi bien une guilde qu'une tribu selon le jeu.
public sealed record GameServerLivePlayer(
    string Name,
    string? Id,
    int? Ping,
    int? Level,
    int? Health,
    int? MaxHealth,
    string? GroupName,
    double? MapX,
    double? MapY,
    GameServerLiveCompanion? Companion);
