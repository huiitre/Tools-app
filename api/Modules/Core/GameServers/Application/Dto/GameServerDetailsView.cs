using System.Text.Json;

namespace Tools.Api.Modules.Core.GameServers.Application.Dto;

// Volet stable du dashboard : chargé à l'ouverture, jamais rafraîchi avec le reste. ServerName,
// GameName et PictureUrl viennent de game_servers et sont donc toujours renseignés ; le provider
// du jeu n'enrichit que ce qu'il sait dire. Null = le jeu ne fournit pas l'information.
public sealed record GameServerDetailsView(
    string ServerName,
    string? GameName,
    string? PictureUrl,
    string? Version,
    string? Description,
    string? WorldId,
    // Codes des commandes d'administration que ce serveur accepte, par exemple « kick » ou
    // « save ». Le front n'affiche que les boutons présents dans cette liste.
    IReadOnlyList<string> Actions,
    IReadOnlyDictionary<string, JsonElement>? Settings);
