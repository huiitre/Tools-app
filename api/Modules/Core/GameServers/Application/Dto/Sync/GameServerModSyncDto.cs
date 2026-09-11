using System.Text.Json.Serialization;

namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;

// Un mod tel que le manifest le décrit. Seul Name est exigé : le reste dépend de l'outil qui a
// produit la liste. Icon, posé à la main, l'emporte sur l'icône de l'hébergeur.
public sealed record GameServerModSyncDto(
    string Name,
    string? Version,
    string? Url,
    IReadOnlyList<string>? Authors,
    [property: JsonPropertyName("filename")] string? FileName,
    string? Icon);
