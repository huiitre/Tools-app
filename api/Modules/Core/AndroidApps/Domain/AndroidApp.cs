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
    DateTimeOffset CreatedAt,
    IReadOnlyList<AndroidAppRelease> Releases)
{
    public AndroidAppRelease? RecommendedRelease =>
        Releases.FirstOrDefault(release => release.Id == RecommendedReleaseId);

    public const int LabelMaxLength = 100;

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
