using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tools.Api.Modules.GameServers.Application.Ports;

// Données de configuration reçues par le flux de synchronisation.
// Le statut de poll reste volontairement hors de ce contrat.
public sealed record GameServerSyncDto(
    string Slug,
    string GameCode,
    string ProtocolType,
    [property: JsonPropertyName("name")] string ServerName,
    int? SteamAppId,
    string? PictureFile,
    string Host,
    int Port,
    JsonElement ProtocolConfig);
