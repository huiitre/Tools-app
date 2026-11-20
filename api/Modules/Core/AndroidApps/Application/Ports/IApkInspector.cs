using Tools.Api.Modules.Core.AndroidApps.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Ports;

// Lit un APK posé sur disque. Un fichier illisible ou dont la signature ne se vérifie pas lève
// ANDROID_APK_INVALID : ce n'est pas un APK qu'une TV pourrait installer.
public interface IApkInspector
{
    Task<ApkInfo> Inspect(string apkPath, CancellationToken cancellationToken = default);
}
