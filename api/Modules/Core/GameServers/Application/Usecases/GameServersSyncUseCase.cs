using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.GameServers.Application.Dto.Sync;
using Tools.Api.Modules.Core.GameServers.Application.Ports.Sync;

namespace Tools.Api.Modules.Core.GameServers.Application.Usecases;

public sealed class GameServersSyncUseCase(
    IGameServerRepository gameServerRepository,
    IGameServersManifestProvider gameServersManifestProvider,
    ISteamAppDetailsProvider steamAppDetailsProvider,
    IGameServerAssetUrlBuilder assetUrlBuilder,
    IEnumerable<IModIconResolver> modIconResolvers,
    ITransactionManager transactionManager)
{
    public async Task<GameServersSyncReport> Execute()
    {
        var gameServers = await gameServersManifestProvider.FetchAsync();
        Validate(gameServers);
        var modIcons = await ResolveModIconsAsync(gameServers);

        var entries = new List<GameServerSyncEntry>(gameServers.Count);
        foreach (var gameServer in gameServers)
        {
            var steamDetails = gameServer.SteamAppId is { } steamAppId
                ? await steamAppDetailsProvider.FindAsync(steamAppId)
                : SteamAppDetailsLookup.AvailableWithoutMetadata;

            var hasLocalPicture = !string.IsNullOrWhiteSpace(gameServer.PictureFile);
            entries.Add(new GameServerSyncEntry(
                gameServer.Slug,
                gameServer.GameCode,
                gameServer.ProtocolType,
                gameServer.ServerName,
                gameServer.SteamAppId,
                gameServer.Host,
                gameServer.Port,
                gameServer.ProtocolConfig.GetRawText(),
                steamDetails.GameName,
                hasLocalPicture ? assetUrlBuilder.Build(gameServer.PictureFile!) : steamDetails.HeaderImageUrl,
                hasLocalPicture,
                steamDetails.IsAvailable,
                gameServer.ClientHost,
                gameServer.ClientPort,
                BuildMods(gameServer, modIcons),
                BuildModpackUrl(gameServer),
                gameServer.ModpackFile is null ? null : gameServer.ModpackSize));
        }

        var created = 0;
        var updated = 0;
        var unchanged = 0;

        await using var transaction = await transactionManager.BeginAsync();
        foreach (var entry in entries)
        {
            var result = await gameServerRepository.UpsertAsync(entry);

            // Une liste de mods modifiée est une mise à jour de configuration comme une autre.
            if (await gameServerRepository.ReplaceModsAsync(entry.Slug, entry.Mods)
                && result == GameServerUpsertResult.Unchanged)
            {
                result = GameServerUpsertResult.Updated;
            }

            switch (result)
            {
                case GameServerUpsertResult.Created:
                    created++;
                    break;
                case GameServerUpsertResult.Updated:
                    updated++;
                    break;
                case GameServerUpsertResult.Unchanged:
                    unchanged++;
                    break;
                default:
                    throw new InvalidOperationException("Résultat d'upsert GameServer inconnu.");
            }
        }

        var deleted = await gameServerRepository.DeleteMissingAsync(entries.Select(entry => entry.Slug).ToArray());
        await transaction.CommitAsync();

        return new GameServersSyncReport(created, updated, unchanged, deleted);
    }

    // Une icône déjà enregistrée n'est jamais redemandée : l'hébergeur n'est interrogé qu'à
    // l'arrivée d'un nouveau mod, et sa panne ne coûte que l'icône des mods encore inconnus.
    private async Task<IReadOnlyDictionary<string, string>> ResolveModIconsAsync(IReadOnlyList<GameServerSyncDto> gameServers)
    {
        var icons = new Dictionary<string, string>(await gameServerRepository.FindModIconsAsync(), StringComparer.Ordinal);
        var unknownUrls = gameServers
            .SelectMany(gameServer => gameServer.Mods ?? [])
            .Where(mod => string.IsNullOrWhiteSpace(mod.Icon)
                          && !string.IsNullOrWhiteSpace(mod.Url)
                          && !icons.ContainsKey(mod.Url))
            .Select(mod => mod.Url!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var resolver in modIconResolvers)
        {
            var supportedUrls = unknownUrls.Where(resolver.Supports).ToList();
            if (supportedUrls.Count == 0)
            {
                continue;
            }

            foreach (var (modUrl, iconUrl) in await resolver.ResolveAsync(supportedUrls))
            {
                icons[modUrl] = iconUrl;
            }
        }

        return icons;
    }

    private static IReadOnlyList<GameServerModEntry> BuildMods(
        GameServerSyncDto gameServer,
        IReadOnlyDictionary<string, string> modIcons) =>
        (gameServer.Mods ?? [])
        .Select(mod => new GameServerModEntry(
            mod.Name,
            mod.Version,
            mod.Url,
            mod.Authors ?? [],
            mod.FileName,
            !string.IsNullOrWhiteSpace(mod.Icon)
                ? mod.Icon
                : mod.Url is not null && modIcons.TryGetValue(mod.Url, out var iconUrl) ? iconUrl : null))
        .ToList();

    // L'URL d'un modpack ne change pas quand il est remplacé : son hash en paramètre empêche un
    // cache de servir l'ancien.
    private string? BuildModpackUrl(GameServerSyncDto gameServer)
    {
        if (string.IsNullOrWhiteSpace(gameServer.ModpackFile))
        {
            return null;
        }

        var url = assetUrlBuilder.Build(gameServer.ModpackFile);
        return string.IsNullOrWhiteSpace(gameServer.ModpackSha256)
            ? url
            : $"{url}?v={Uri.EscapeDataString(gameServer.ModpackSha256)}";
    }

    private static void Validate(IReadOnlyList<GameServerSyncDto> gameServers)
    {
        var slugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var gameServer in gameServers)
        {
            if (string.IsNullOrWhiteSpace(gameServer.Slug)
                || !System.Text.RegularExpressions.Regex.IsMatch(gameServer.Slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            {
                throw AppException.Validation("INVALID_GAME_SERVER_SLUG", "Le slug doit être un nom de dossier NAS en minuscules.");
            }

            if (!slugs.Add(gameServer.Slug))
            {
                throw AppException.Validation("DUPLICATE_GAME_SERVER_SLUG", "Chaque slug ne peut apparaître qu'une seule fois dans un sync.");
            }

            if (string.IsNullOrWhiteSpace(gameServer.GameCode)
                || string.IsNullOrWhiteSpace(gameServer.ProtocolType)
                || string.IsNullOrWhiteSpace(gameServer.ServerName)
                || string.IsNullOrWhiteSpace(gameServer.Host))
            {
                throw AppException.Validation("INVALID_GAME_SERVER", "Les champs gameCode, protocolType, name et host sont obligatoires.");
            }

            if (gameServer.SteamAppId is <= 0 || gameServer.Port is < 1 or > 65535)
            {
                throw AppException.Validation("INVALID_GAME_SERVER", "steamAppId doit être positif et port doit être entre 1 et 65535.");
            }

            if (string.IsNullOrWhiteSpace(gameServer.ClientHost) || gameServer.ClientPort is < 1 or > 65535)
            {
                throw AppException.Validation("INVALID_GAME_SERVER", "clientHost est obligatoire et clientPort doit être entre 1 et 65535.");
            }

            if (gameServer.ProtocolConfig.ValueKind is not System.Text.Json.JsonValueKind.Object)
            {
                throw AppException.Validation("INVALID_PROTOCOL_CONFIG", "protocolConfig doit être un objet JSON.");
            }

            if (!string.IsNullOrWhiteSpace(gameServer.PictureFile) && !IsDirectFileOf(gameServer.PictureFile, "img/"))
            {
                throw AppException.Validation("INVALID_PICTURE_FILE", "pictureFile doit désigner un fichier direct du dossier img.");
            }

            if (!string.IsNullOrWhiteSpace(gameServer.ModpackFile) && !IsDirectFileOf(gameServer.ModpackFile, "modpacks/"))
            {
                throw AppException.Validation("INVALID_MODPACK_FILE", "modpackFile doit désigner un fichier direct du dossier modpacks.");
            }

            if (gameServer.Mods?.Any(mod => mod is null || string.IsNullOrWhiteSpace(mod.Name)) == true)
            {
                throw AppException.Validation("INVALID_GAME_SERVER_MOD", "Chaque mod doit porter un nom.");
            }
        }
    }

    private static bool IsDirectFileOf(string file, string directory)
    {
        if (!file.StartsWith(directory, StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = file[directory.Length..];
        return !string.IsNullOrWhiteSpace(fileName)
               && Path.GetFileName(fileName) == fileName
               && !fileName.Contains("..", StringComparison.Ordinal);
    }
}
