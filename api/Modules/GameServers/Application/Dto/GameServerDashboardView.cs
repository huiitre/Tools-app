namespace Tools.Api.Modules.GameServers.Application.Dto;

// Contrat public du widget dashboard. Les données de connexion (host, port, protocolConfig) et
// le slug d'infrastructure n'en font volontairement pas partie.
public sealed record GameServerDashboardView(
    string GameName,
    string ServerName,
    string? PictureUrl,
    bool? Online,
    int? NumPlayers,
    int? MaxPlayers,
    DateTimeOffset? CheckedAt);
