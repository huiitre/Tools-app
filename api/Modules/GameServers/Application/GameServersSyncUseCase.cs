using Tools.Api.Modules.Common.Application.Exceptions;
using Tools.Api.Modules.Common.Application.Ports;
using Tools.Api.Modules.GameServers.Application.Dto;
using Tools.Api.Modules.GameServers.Application.Ports;

namespace Tools.Api.Modules.GameServers.Application;

public sealed class GameServersSyncUseCase(
    IGameServerRepository gameServerRepository,
    IGameServersManifestProvider gameServersManifestProvider,
    ISteamAppDetailsProvider steamAppDetailsProvider,
    IGameServerImageUrlBuilder imageUrlBuilder,
    ITransactionManager transactionManager)
{
    public async Task<GameServersSyncReport> Execute()
    {
        var gameServers = await gameServersManifestProvider.FetchAsync();
        Validate(gameServers);

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
                hasLocalPicture ? imageUrlBuilder.Build(gameServer.PictureFile!) : steamDetails.HeaderImageUrl,
                hasLocalPicture,
                steamDetails.IsAvailable,
                gameServer.ClientHost,
                gameServer.ClientPort));
        }

        var created = 0;
        var updated = 0;
        var unchanged = 0;

        await using var transaction = await transactionManager.BeginAsync();
        foreach (var entry in entries)
        {
            switch (await gameServerRepository.UpsertAsync(entry))
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

            if (!string.IsNullOrWhiteSpace(gameServer.PictureFile) && !IsLocalPicturePath(gameServer.PictureFile))
            {
                throw AppException.Validation("INVALID_PICTURE_FILE", "pictureFile doit désigner un fichier direct du dossier img.");
            }
        }
    }

    private static bool IsLocalPicturePath(string pictureFile)
    {
        const string imageDirectory = "img/";
        if (!pictureFile.StartsWith(imageDirectory, StringComparison.Ordinal))
        {
            return false;
        }

        var fileName = pictureFile[imageDirectory.Length..];
        return !string.IsNullOrWhiteSpace(fileName)
               && Path.GetFileName(fileName) == fileName
               && !fileName.Contains("..", StringComparison.Ordinal);
    }
}
