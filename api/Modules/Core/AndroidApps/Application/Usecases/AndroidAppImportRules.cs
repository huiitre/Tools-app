using Tools.Api.Modules.Core.AndroidApps.Application.Views;
using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// Règles communes à l'analyse et à la confirmation : la confirmation relit l'APK et les rejoue,
// rien de ce que le front renvoie n'est cru.
public static class AndroidAppImportRules
{
    public const string NewApp = "NEW_APP";
    public const string CertificateChanged = "CERTIFICATE_CHANGED";
    public const string OlderThanRecommended = "OLDER_THAN_RECOMMENDED";
    public const string MinSdkRaised = "MIN_SDK_RAISED";

    // Seul le doublon est refusé : l'ordre d'import est libre (une stable peut arriver après une
    // nightly de numéro supérieur). C'est la TV qui refuse un retour en arrière.
    public static void EnsureVersionIsNew(AndroidApp? existing, ApkInfo apk)
    {
        if (existing?.Releases.Any(release => release.VersionCode == apk.VersionCode) == true)
        {
            throw AppException.Conflict(
                "ANDROID_APP_VERSION_EXISTS",
                $"La version {apk.VersionCode} de {apk.PackageName} est déjà importée.");
        }
    }

    public static string DefaultLabel(ApkInfo apk)
    {
        var label = apk.Label ?? apk.PackageName;

        return label.Length <= AndroidApp.LabelMaxLength ? label : label[..AndroidApp.LabelMaxLength];
    }

    public static List<AndroidAppImportWarningView> Warnings(AndroidApp? existing, ApkInfo apk)
    {
        var warnings = new List<AndroidAppImportWarningView>();

        if (existing is null)
        {
            warnings.Add(new AndroidAppImportWarningView(
                NewApp, $"{apk.PackageName} n'est pas encore suivi : l'app sera créée."));

            return warnings;
        }

        var recommended = existing.RecommendedRelease;

        if (recommended is null)
        {
            return warnings;
        }

        if (apk.VersionCode < recommended.VersionCode)
        {
            warnings.Add(new AndroidAppImportWarningView(
                OlderThanRecommended,
                $"Cette version ({apk.VersionCode}) est plus ancienne que celle proposée sur le store "
                + $"({recommended.VersionCode}). Un appareil qui a déjà la version proposée ne pourra pas revenir "
                + "en arrière : Android refuse les retours en arrière."));
        }

        if (apk.MinSdk > recommended.MinSdk)
        {
            warnings.Add(new AndroidAppImportWarningView(
                MinSdkRaised,
                $"Cette version exige un Android plus récent (API {apk.MinSdk}) que celle proposée sur le store "
                + $"(API {recommended.MinSdk}) : les appareils les plus anciens ne pourront plus l'installer."));
        }

        if (!recommended.CertSha256.AsSpan().SequenceEqual(apk.CertSha256))
        {
            warnings.Add(new AndroidAppImportWarningView(
                CertificateChanged,
                $"Le certificat ({Convert.ToHexStringLower(apk.CertSha256)}) diffère de celui de la version "
                + $"proposée sur le store ({Convert.ToHexStringLower(recommended.CertSha256)}). Un appareil qui a l'app "
                + "devra la désinstaller puis la réinstaller, et perdra ses données."));
        }

        return warnings;
    }
}
