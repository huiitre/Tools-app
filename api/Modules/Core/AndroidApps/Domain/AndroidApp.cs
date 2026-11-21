using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Core.AndroidApps.Domain;

// App tierce suivie par huihuistore, avec ses releases de la plus récente à la plus ancienne.
public sealed record AndroidApp(
    long Id,
    string PackageName,
    string Label,
    int DisplayOrder,
    bool IsActive,
    long? RecommendedReleaseId,
    // Notes libres de l'administrateur (URL de téléchargement…), jamais exposées au catalogue public.
    string? AdminNotes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AndroidAppRelease> Releases)
{
    public AndroidAppRelease? RecommendedRelease =>
        Releases.FirstOrDefault(release => release.Id == RecommendedReleaseId);

    public const int LabelMaxLength = 100;
    public const int AdminNotesMaxLength = 5000;

    // Des notes vides valent « aucune note » : on stocke NULL plutôt qu'une chaîne blanche.
    public static string? NormalizeAdminNotes(string notes)
    {
        var trimmed = notes.Trim();

        if (trimmed.Length > AdminNotesMaxLength)
        {
            throw AppException.Validation(
                "ANDROID_APP_ADMIN_NOTES_TOO_LONG",
                $"Les notes ne doivent pas dépasser {AdminNotesMaxLength} caractères.");
        }

        return trimmed.Length == 0 ? null : trimmed;
    }

    // Le libellé est affiché sur la TV : jamais vide, borné par la colonne.
    public static string NormalizeLabel(string? label)
    {
        var trimmed = label?.Trim() ?? "";

        if (trimmed.Length == 0 || trimmed.Length > LabelMaxLength)
        {
            throw AppException.Validation(
                "ANDROID_APP_LABEL_INVALID",
                $"Le libellé doit faire entre 1 et {LabelMaxLength} caractères.");
        }

        return trimmed;
    }
}

public sealed record AndroidAppRelease(
    long Id,
    long AppId,
    long VersionCode,
    string? VersionName,
    int MinSdk,
    string FilePath,
    long FileSize,
    byte[] FileSha256,
    byte[] CertSha256,
    string? ReleaseNotes,
    DateTimeOffset UploadedAt,
    long UploadedBy,
    string? UploadedByName);
