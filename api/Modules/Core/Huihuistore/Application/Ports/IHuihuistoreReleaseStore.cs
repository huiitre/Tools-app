using Tools.Api.Modules.Core.Huihuistore.Domain;

namespace Tools.Api.Modules.Core.Huihuistore.Application.Ports;

// Fichiers de huihuistore dans tools_assets : tools_core/huihuistore/<env>/<versionCode>.apk et
// version.json, qui désigne celui qui est publié.
public interface IHuihuistoreReleaseStore
{
    // La version publiée, null si rien ne l'a encore été.
    Task<HuihuistoreRelease?> FindCurrent(CancellationToken cancellationToken = default);

    // Chemin d'un fichier temporaire hors de tools_assets, où recevoir l'APK avant de le lire.
    string NewStagingPath();

    // Range l'APK sous son versionCode, puis réécrit version.json en dernier : un appareil ne lit
    // jamais un manifeste qui annonce un fichier absent. Garde les N dernières versions.
    Task Publish(string stagedApkPath, HuihuistoreRelease release, CancellationToken cancellationToken = default);

    // Chemin relatif à tools_assets d'un fichier publié, pour en construire l'URL.
    string RelativePath(string fileName);
}
