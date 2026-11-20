namespace Tools.Api.Modules.Core.AndroidApps.Domain;

// Ce qu'un APK dit de lui-même, lu par les outils du SDK Android : rien n'est saisi à la main.
public sealed record ApkInfo(
    string PackageName,
    long VersionCode,
    string? VersionName,
    int MinSdk,
    string? Label,
    long FileSize,
    byte[] FileSha256,
    byte[] CertSha256,
    string CertSubject,
    IReadOnlyList<string> SignatureSchemes);
