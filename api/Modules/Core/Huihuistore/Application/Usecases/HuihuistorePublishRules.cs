using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Huihuistore.Domain;

namespace Tools.Api.Modules.Core.Huihuistore.Application.Usecases;

// Ce qu'un APK doit être pour devenir la version publiée de huihuistore. Tout est lu dans l'APK :
// rien de ce que la CI déclare n'est cru.
public static class HuihuistorePublishRules
{
    public static void Validate(ApkInfo apk, string expectedPackageName, string expectedCertSha256, HuihuistoreRelease? current)
    {
        if (string.IsNullOrWhiteSpace(expectedPackageName) || string.IsNullOrWhiteSpace(expectedCertSha256))
        {
            throw AppException.Unavailable(
                "HUIHUISTORE_NOT_CONFIGURED",
                "Le paquet et le certificat attendus de huihuistore ne sont pas configurés sur ce serveur.");
        }

        // Le flavor d'un autre environnement (ex. l'APK QA envoyé à la prod) est refusé ici.
        if (apk.PackageName != expectedPackageName)
        {
            throw AppException.Validation(
                "HUIHUISTORE_PACKAGE_MISMATCH",
                $"Ce serveur publie {expectedPackageName}, pas {apk.PackageName}.");
        }

        if (!string.Equals(Convert.ToHexStringLower(apk.CertSha256), expectedCertSha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.Validation(
                "HUIHUISTORE_CERTIFICATE_MISMATCH",
                "L'APK n'est pas signé avec la clé de huihuistore de cet environnement.");
        }

        // Contrairement aux apps du store, l'ordre compte : les appareils se mettent à jour seuls et
        // Android refuse un retour en arrière. Un envoi en retard ne doit pas écraser un plus récent.
        if (current is not null && apk.VersionCode <= current.VersionCode)
        {
            throw AppException.Conflict(
                "HUIHUISTORE_VERSION_NOT_NEWER",
                $"La version {apk.VersionCode} n'est pas plus récente que la version publiée ({current.VersionCode}).");
        }
    }
}
