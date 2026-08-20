namespace Tools.Api.Modules.Core.GameServers.Application.Dto;

// Contrat public du widget dashboard. host/port/protocolConfig (poll interne, souvent une IP
// LAN et des credentials) et le slug d'infrastructure n'en font volontairement pas partie.
// ClientHost/ClientPort sont différents : c'est l'adresse publique à laquelle un joueur se
// connecte, donc destinée à être affichée.
public sealed record GameServerDashboardView(
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
