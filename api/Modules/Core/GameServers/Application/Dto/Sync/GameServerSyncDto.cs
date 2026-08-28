using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;

// Forme lue dans gameservers.json, produit consolidé de l'extractor NAS.
public sealed record GameServerSyncDto(
    string Slug,
    string GameCode,
    string ProtocolType,
    [property: JsonPropertyName("name")] string ServerName,
    int? SteamAppId,
    string? PictureFile,
    string Host,
    int Port,
    string ClientHost,
    int ClientPort,
    JsonElement ProtocolConfig);
