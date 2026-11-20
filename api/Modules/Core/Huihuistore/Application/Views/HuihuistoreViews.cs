namespace Tools.Api.Modules.Core.Huihuistore.Application.Views;

// Lu par huihuistore pour savoir s'il doit se mettre à jour. Empreintes en hexadécimal minuscule.
public sealed record HuihuistoreUpdateView(
    string PackageName,
    long VersionCode,
    string? VersionName,
    int MinSdk,
    string Url,
    long Size,
    string Sha256,
    string CertSha256,
    string? ReleaseNotes,
    DateTimeOffset PublishedAt);
