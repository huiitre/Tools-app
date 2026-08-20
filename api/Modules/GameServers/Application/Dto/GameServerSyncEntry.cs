namespace Tools.Api.Modules.GameServers.Application.Dto;

// Manifest enrichi avant persistance. La disponibilité Steam permet de conserver les
// métadonnées connues pendant une panne réseau, sans confondre cette panne avec une valeur null.
public sealed record GameServerSyncEntry(
    string Slug,
    string GameCode,
    string ProtocolType,
    string ServerName,
    int? SteamAppId,
    string Host,
    int Port,
    string ProtocolConfig,
    string? GameName,
    string? PictureUrl,
    bool HasLocalPicture,
    bool SteamMetadataAvailable,
    string ClientHost,
    int ClientPort);
