using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.AndroidApps.Infrastructure;

// Zone d'attente sur le disque local de l'API, fichiers définitifs dans tools_assets :
//   tools_core/android_apps/<env>/<package>/<versionCode>.apk
//
// Le nom de paquet et le versionCode viennent de l'APK, déjà validés (format Android, entier) :
// ils ne peuvent pas faire sortir un chemin de son dossier. Le contrôle est refait ici quand même.
public sealed class FileSystemAndroidAppFileStore(
    IOptions<AndroidAppsOptions> options,
    ILogger<FileSystemAndroidAppFileStore> logger) : IAndroidAppFileStore
{
    public TimeSpan StagingLifetime { get; } = TimeSpan.FromHours(1);

    private AndroidAppsOptions Options => options.Value;

    public async Task<Guid> Stage(Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Options.StagingPath);
        PurgeExpired();

        var importId = Guid.NewGuid();
        var path = StagedPath(importId);

        await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return importId;
    }

    public string? FindStaged(Guid importId)
    {
        var path = StagedPath(importId);

        if (!File.Exists(path))
        {
            return null;
        }

        if (File.GetLastWriteTimeUtc(path) + StagingLifetime < DateTime.UtcNow)
        {
            File.Delete(path);
            return null;
        }

        return path;
    }

    public DateTimeOffset StagedExpiresAt(Guid importId) =>
        new DateTimeOffset(File.GetLastWriteTimeUtc(StagedPath(importId)), TimeSpan.Zero) + StagingLifetime;

    public void DeleteStaged(Guid importId)
    {
        var path = StagedPath(importId);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public string Publish(Guid importId, string packageName, long versionCode)
    {
        var source = FindStaged(importId)
            ?? throw AppException.NotFound("ANDROID_APP_IMPORT_NOT_FOUND", "Import introuvable ou expiré.");

        var relativePath = $"{AppFolder(packageName)}/{versionCode}.apk";
        var destination = Absolute(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        // Déplacement entre deux volumes (zone d'attente locale → montage de tools_assets) : .NET
        // copie puis supprime. Le fichier n'est publié au catalogue qu'après, par la base.
        File.Move(source, destination, overwrite: true);

        return relativePath;
    }

    public void Delete(string relativePath)
    {
        var path = Absolute(relativePath);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void DeleteAppFolderIfEmpty(string packageName)
    {
        var folder = Absolute(AppFolder(packageName));

        if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
        {
            Directory.Delete(folder);
        }
    }

    private string AppFolder(string packageName)
    {
        if (string.IsNullOrWhiteSpace(Options.Environment))
        {
            throw AppException.Unavailable(
                "ANDROID_APPS_STORAGE_MISSING", "Le dossier d'environnement des APK n'est pas configuré.");
        }

        return $"tools_core/android_apps/{Options.Environment}/{packageName}";
    }

    // Tout chemin résolu doit rester sous le dossier des APK de cet environnement.
    private string Absolute(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(Options.StorageRoot, "tools_core", "android_apps", Options.Environment));
        var path = Path.GetFullPath(Path.Combine(Options.StorageRoot, relativePath));

        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Chemin hors du dossier des APK : {relativePath}");
        }

        return path;
    }

    private string StagedPath(Guid importId) => Path.Combine(Options.StagingPath, $"{importId:N}.apk");

    private void PurgeExpired()
    {
        foreach (var file in Directory.EnumerateFiles(Options.StagingPath, "*.apk"))
        {
            if (File.GetLastWriteTimeUtc(file) + StagingLifetime >= DateTime.UtcNow)
            {
                continue;
            }

            try
            {
                File.Delete(file);
            }
            catch (IOException exception)
            {
                logger.LogWarning(exception, "Import expiré impossible à supprimer : {File}", file);
            }
        }
    }
}
