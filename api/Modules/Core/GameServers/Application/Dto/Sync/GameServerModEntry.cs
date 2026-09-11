namespace Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;

// Mod prêt à être enregistré, icône résolue. L'égalité compare les auteurs un à un : c'est elle
// qui décide si la liste d'un serveur a changé depuis le sync précédent.
public sealed record GameServerModEntry(
    string Name,
    string? Version,
    string? Url,
    IReadOnlyList<string> Authors,
    string? FileName,
    string? IconUrl)
{
    public bool Equals(GameServerModEntry? other) =>
        other is not null
        && Name == other.Name
        && Version == other.Version
        && Url == other.Url
        && FileName == other.FileName
        && IconUrl == other.IconUrl
        && Authors.SequenceEqual(other.Authors);

    public override int GetHashCode() => HashCode.Combine(Name, Version, Url, FileName, IconUrl);
}
