namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;

// Contrat public du widget. host/port/protocolConfig (poll interne, souvent une IP LAN et des
// credentials) n'en font volontairement pas partie. ClientHost/ClientPort sont différents :
// c'est l'adresse publique à laquelle un joueur se connecte, donc destinée à être affichée.
// Slug est exposé parce que le front en a besoin pour appeler le dashboard du serveur.
public sealed record GameServerDashboardView(
    string Slug,
    // Le front en a besoin pour choisir l'implémentation de carte du jeu.
    string GameCode,
    // Décidé par la présence d'un provider de dashboard dans le module, jamais par la base.
    bool HasDashboard,
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
