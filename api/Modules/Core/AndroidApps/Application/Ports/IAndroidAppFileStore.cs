namespace Tools.Api.Modules.Core.AndroidApps.Application.Ports;

// Fichiers APK : la zone d'attente des imports non confirmés, puis leur place définitive dans
// tools_assets. Les chemins définitifs sont relatifs à la racine de tools_assets.
public interface IAndroidAppFileStore
{
    // Durée de vie d'un import non confirmé.
    TimeSpan StagingLifetime { get; }

    // Écrit le fichier reçu en attente et renvoie l'identifiant de l'import. Supprime au passage
    // les imports expirés : pas de tâche de fond pour si peu.
    Task<Guid> Stage(Stream content, CancellationToken cancellationToken = default);

    // Chemin du fichier en attente, null s'il n'existe pas ou a expiré.
    string? FindStaged(Guid importId);

    DateTimeOffset StagedExpiresAt(Guid importId);

    void DeleteStaged(Guid importId);

    // Déplace le fichier en attente à sa place définitive et renvoie son chemin relatif.
    string Publish(Guid importId, string packageName, long versionCode);

    // Sans erreur si le fichier n'existe déjà plus.
    void Delete(string relativePath);

    // Supprime le dossier d'une app s'il est vide.
    void DeleteAppFolderIfEmpty(string packageName);
}
