using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Views;

// Empreintes en hexadécimal minuscule : le format qu'affiche apksigner, simple à comparer côté
// Android.
internal static class Hex
{
    public static string Of(byte[] bytes) => Convert.ToHexStringLower(bytes);
}

// --- Administration --------------------------------------------------------

public sealed record AndroidAppReleaseView(
    long Id,
    long VersionCode,
    string? VersionName,
    int MinSdk,
    string Url,
    long FileSize,
    string FileSha256,
    string CertSha256,
    string? ReleaseNotes,
    DateTimeOffset UploadedAt,
    long UploadedBy,
    string? UploadedByName,
    bool IsRecommended)
{
    public static AndroidAppReleaseView From(AndroidAppRelease release, long? recommendedReleaseId, IAndroidAppAssetUrlBuilder urls) => new(
        release.Id,
        release.VersionCode,
        release.VersionName,
        release.MinSdk,
        urls.Build(release.FilePath),
        release.FileSize,
        Hex.Of(release.FileSha256),
        Hex.Of(release.CertSha256),
        release.ReleaseNotes,
        release.UploadedAt,
        release.UploadedBy,
        release.UploadedByName,
        release.Id == recommendedReleaseId);
}

public sealed record AndroidAppView(
    long Id,
    string PackageName,
    string Label,
    int DisplayOrder,
    bool IsActive,
    long? RecommendedReleaseId,
    string? AdminNotes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AndroidAppReleaseView> Releases)
{
    public static AndroidAppView From(AndroidApp app, IAndroidAppAssetUrlBuilder urls) => new(
        app.Id,
        app.PackageName,
        app.Label,
        app.DisplayOrder,
        app.IsActive,
        app.RecommendedReleaseId,
        app.AdminNotes,
        app.CreatedAt,
        app.Releases.Select(release => AndroidAppReleaseView.From(release, app.RecommendedReleaseId, urls)).ToList());
}

// --- Import ----------------------------------------------------------------

public sealed record ApkInfoView(
    string PackageName,
    long VersionCode,
    string? VersionName,
    int MinSdk,
    string? Label,
    long FileSize,
    string FileSha256,
    string CertSha256,
    string CertSubject,
    IReadOnlyList<string> SignatureSchemes)
{
    public static ApkInfoView From(ApkInfo apk) => new(
        apk.PackageName,
        apk.VersionCode,
        apk.VersionName,
        apk.MinSdk,
        apk.Label,
        apk.FileSize,
        Hex.Of(apk.FileSha256),
        Hex.Of(apk.CertSha256),
        apk.CertSubject,
        apk.SignatureSchemes);
}

// Ce que la popup affiche ; elle s'ouvre à chaque import.
public sealed record AndroidAppImportWarningView(string Code, string Message);

// La version proposée aux TV, pour que la popup compare l'APK importé à ce qui est en place.
public sealed record AndroidAppImportExistingAppView(
    long Id,
    string Label,
    long? RecommendedVersionCode,
    string? RecommendedVersionName,
    int? RecommendedMinSdk,
    long? RecommendedFileSize,
    string? RecommendedCertSha256);

public sealed record AndroidAppImportAnalysisView(
    Guid ImportId,
    DateTimeOffset ExpiresAt,
    ApkInfoView Apk,
    AndroidAppImportExistingAppView? ExistingApp,
    string SuggestedLabel,
    IReadOnlyList<AndroidAppImportWarningView> Warnings);

// --- Catalogue (TV) --------------------------------------------------------

public sealed record AndroidAppCatalogueReleaseView(
    long VersionCode,
    string? VersionName,
    int MinSdk,
    string Url,
    long Size,
    string Sha256,
    string CertSha256,
    string? ReleaseNotes);

public sealed record AndroidAppCatalogueAppView(
    string PackageName,
    string Label,
    AndroidAppCatalogueReleaseView Release);

public sealed record AndroidAppCatalogueView(IReadOnlyList<AndroidAppCatalogueAppView> Apps);
