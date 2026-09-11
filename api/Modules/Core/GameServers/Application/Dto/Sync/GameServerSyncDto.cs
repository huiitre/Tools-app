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
    JsonElement ProtocolConfig,
    // Null pour un serveur sans mods, ou sans modpack à télécharger. ModpackFile désigne un fichier du dossier modpacks/, dont ModpackSha256 est la version.
    IReadOnlyList<GameServerModSyncDto>? Mods = null,
    string? ModpackFile = null,
    long? ModpackSize = null,
    string? ModpackSha256 = null);
