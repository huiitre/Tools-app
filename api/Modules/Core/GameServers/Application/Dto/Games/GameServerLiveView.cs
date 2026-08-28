namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Volet rafraîchi du dashboard, rendu par un seul appel quel que soit le jeu : le provider
// enchaîne en interne les requêtes dont il a besoin. Chaque champ est optionnel — un jeu qui
// n'expose pas les FPS laisse simplement le champ à null et le front affiche « indisponible ».
// La carte n'est pas ici : elle reste propre à chaque jeu et vit dans son module.
public sealed record GameServerLiveView(
    int? PlayerCount,
    int? MaxPlayers,
    double? Fps,
    double? AverageFps,
    double? FrameTimeMs,
    long? UptimeSeconds,
    int? InGameDay,
    int? BaseCount,
    IReadOnlyList<GameServerLivePlayer> Players,
    // Constructions des joueurs. Vide quand le jeu n'en expose pas.
    IReadOnlyList<GameServerLiveStructure> Structures,
    // Journal du serveur, des plus anciennes lignes aux plus récentes. Vide quand le jeu n'en
    // expose pas — et attention, certains le vident à la lecture (Ark).
    IReadOnlyList<string> Log,
    // Sections que le provider n'a pas pu récupérer pendant cet appel, alors que le jeu sait
    // normalement les fournir. Distingue une panne d'une information que le jeu n'a jamais.
    IReadOnlyList<string> Unavailable);
