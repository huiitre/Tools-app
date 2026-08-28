using System.Text.Json;

namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

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
    // Réglages du serveur, tels que le jeu les nomme : ni les clés ni les types ne sont communs
    // d'un jeu à l'autre, d'où un dictionnaire de valeurs brutes plutôt qu'un contrat figé.
    IReadOnlyDictionary<string, JsonElement>? Settings,
    // Actions que ce serveur accepte, filtrées sur les droits de l'appelant : ce qui est absent
    // d'ici lui est refusé.
    IReadOnlyList<GameServerActionDefinition> Actions);
