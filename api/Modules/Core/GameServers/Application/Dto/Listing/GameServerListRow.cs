namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;

// Projection SQL du widget, interne au module. Elle porte le gameCode dont le use case a besoin
// pour savoir si le jeu a un dashboard ; la vue publique, elle, ne l'expose pas.
public sealed record GameServerListRow(
    string Slug,
    string GameCode,
    string GameName,
    string ServerName,
    string? PictureUrl,
    bool? Online,
    int? NumPlayers,
    int? MaxPlayers,
    DateTime? CheckedAt,
    string? ClientHost,
    int? ClientPort
);
