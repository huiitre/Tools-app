namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;

// IconUrl vient du manifest ou de l'hébergeur du mod ; null si aucun des deux n'en fournit.
public sealed record GameServerModView(
    string Name,
    string? Version,
    string? Url,
    IReadOnlyList<string> Authors,
    string? FileName,
    string? IconUrl);
