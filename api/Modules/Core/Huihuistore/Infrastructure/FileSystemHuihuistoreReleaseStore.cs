using System.Text.Json;
using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.AndroidApps.Infrastructure;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Huihuistore.Application.Ports;
using Tools.Api.Modules.Core.Huihuistore.Domain;

namespace Tools.Api.Modules.Core.Huihuistore.Infrastructure;

// tools_core/huihuistore/<env>/
//   <versionCode>.apk   une par version, les N dernières gardées
//   version.json        la version publiée
//
// Chaque version garde son propre nom de fichier plutôt qu'un « huihuistore.apk » écrasé : l'APK
// est en place avant que version.json ne le désigne, et un appareil qui télécharge pendant une
// publication obtient toujours le fichier que son manifeste annonce.
public sealed class FileSystemHuihuistoreReleaseStore(
    IOptions<AndroidAppsOptions> storageOptions,
    IOptions<HuihuistoreOptions> options,
    ILogger<FileSystemHuihuistoreReleaseStore> logger) : IHuihuistoreReleaseStore
{
    private const string ManifestName = "version.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    // Une publication à la fois : la CI les sérialise déjà (concurrency), ceci couvre un appel manuel.
    private static readonly SemaphoreSlim PublishLock = new(1, 1);

    public async Task<HuihuistoreRelease?> FindCurrent(CancellationToken cancellationToken = default)
    {
        var manifest = Path.Combine(Folder(), ManifestName);

        if (!File.Exists(manifest))
        {
            return null;
        }

        await using var stream = File.OpenRead(manifest);

        return await JsonSerializer.DeserializeAsync<HuihuistoreRelease>(stream, Json, cancellationToken);
    }

    public string NewStagingPath()
    {
        Directory.CreateDirectory(storageOptions.Value.StagingPath);

        return Path.Combine(storageOptions.Value.StagingPath, $"huihuistore-{Guid.NewGuid():N}.apk");
    }

    public async Task Publish(string stagedApkPath, HuihuistoreRelease release, CancellationToken cancellationToken = default)
    {
        await PublishLock.WaitAsync(cancellationToken);

        try
        {
            var folder = Folder();
            Directory.CreateDirectory(folder);

            File.Move(stagedApkPath, Path.Combine(folder, release.FileName), overwrite: true);

            // Écrit à côté puis renommé : le renommage remplace version.json d'un coup.
            var temporary = Path.Combine(folder, $".{ManifestName}.tmp");

            await using (var stream = File.Create(temporary))
            {
                await JsonSerializer.SerializeAsync(stream, release, Json, cancellationToken);
            }

            File.Move(temporary, Path.Combine(folder, ManifestName), overwrite: true);

            PruneOldReleases(folder, release.FileName);
        }
        finally
        {
            PublishLock.Release();
        }
    }

    public string RelativePath(string fileName) => $"{RelativeFolder()}/{fileName}";

    // Garde les N versions les plus récentes ; la version publiée n'est jamais supprimée.
    private void PruneOldReleases(string folder, string currentFileName)
    {
        var kept = Math.Max(1, options.Value.KeptReleases);

        var obsolete = Directory.EnumerateFiles(folder, "*.apk")
            .Select(path => (Path: path, VersionCode: long.TryParse(Path.GetFileNameWithoutExtension(path), out var code) ? code : -1))
            .Where(file => file.VersionCode >= 0 && Path.GetFileName(file.Path) != currentFileName)
            .OrderByDescending(file => file.VersionCode)
            .Skip(kept - 1);

        foreach (var file in obsolete)
        {
            try
            {
                File.Delete(file.Path);
            }
            catch (IOException exception)
            {
                logger.LogWarning(exception, "Ancienne version de huihuistore impossible à supprimer : {File}", file.Path);
            }
        }
    }

    private string RelativeFolder()
    {
        var environment = storageOptions.Value.Environment;

        if (string.IsNullOrWhiteSpace(environment))
        {
            throw AppException.Unavailable(
                "HUIHUISTORE_STORAGE_MISSING", "Le dossier d'environnement de huihuistore n'est pas configuré.");
        }

        return $"tools_core/huihuistore/{environment}";
    }

    private string Folder() => Path.GetFullPath(Path.Combine(storageOptions.Value.StorageRoot, RelativeFolder()));
}
