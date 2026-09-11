namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Listing;

// Contrat de GET /gameservers/{slug}/mods. ModpackUrl est null quand les joueurs n'ont rien à
// télécharger, par exemple quand le jeu installe lui-même les mods à la connexion.
public sealed record GameServerModsView(
    string? ModpackUrl,
    long? ModpackSize,
    IReadOnlyList<GameServerModView> Mods);
