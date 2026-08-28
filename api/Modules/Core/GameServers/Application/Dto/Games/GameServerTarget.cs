namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Games;

// Serveur à interroger, tel que le porte game_servers. ProtocolConfig contient les credentials :
// cette projection est réservée aux providers et ne sort jamais de l'API.
public sealed record GameServerTarget(
    long Id,
    string Slug,
    string GameCode,
    string ServerName,
    string? GameName,
    string? PictureUrl,
    string Host,
    int Port,
    string ProtocolConfig);
