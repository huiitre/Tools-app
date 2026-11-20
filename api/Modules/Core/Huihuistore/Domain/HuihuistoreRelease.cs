namespace Tools.Api.Modules.Core.Huihuistore.Domain;

// La version publiée de huihuistore : le contenu de version.json, à côté des APK. Pas de base :
// huihuistore n'est pas une app du store, et le fichier suffit à dire ce qui est en ligne.
public sealed record HuihuistoreRelease(
    string PackageName,
    long VersionCode,
    string? VersionName,
    int MinSdk,
    string FileName,
    long Size,
    string Sha256,
    string CertSha256,
    string? ReleaseNotes,
    DateTimeOffset PublishedAt);
