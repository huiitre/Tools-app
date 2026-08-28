namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Construction posée par les joueurs (base Palworld, et l'équivalent des autres jeux). Positions
// brutes du jeu : c'est le front qui sait les projeter sur une carte, pas l'API.
public sealed record GameServerLiveStructure(
    string Key,
    string Name,
    string? GroupId,
    string? GroupName,
    double PositionX,
    double PositionY,
    // Créatures rattachées à cette construction (les pals d'une base chez Palworld). Null quand
    // le jeu n'en a pas la notion.
    int? CreatureCount);
