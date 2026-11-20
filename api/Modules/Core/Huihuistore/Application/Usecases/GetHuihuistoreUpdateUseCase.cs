using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Huihuistore.Application.Ports;
using Tools.Api.Modules.Core.Huihuistore.Application.Views;
using Tools.Api.Modules.Core.Huihuistore.Domain;

namespace Tools.Api.Modules.Core.Huihuistore.Application.Usecases;

// Lu par huihuistore au lancement puis régulièrement tant qu'il est ouvert. Volontairement non
// sécurisé, comme le catalogue : l'appareil n'a pas de compte, et rien ici n'est secret.
public sealed class GetHuihuistoreUpdateUseCase(
    IHuihuistoreReleaseStore releaseStore,
    IAndroidAppAssetUrlBuilder urls)
{
    public async Task<HuihuistoreUpdateView> Execute(CancellationToken cancellationToken)
    {
        var release = await releaseStore.FindCurrent(cancellationToken)
            ?? throw AppException.NotFound("HUIHUISTORE_NOT_PUBLISHED", "Aucune version de huihuistore n'est publiée.");

        return ToView(release, releaseStore, urls);
    }

    // L'APK est téléchargé sur les assets, comme ceux du store, jamais via l'API.
    internal static HuihuistoreUpdateView ToView(
        HuihuistoreRelease release,
        IHuihuistoreReleaseStore releaseStore,
        IAndroidAppAssetUrlBuilder urls) => new(
        release.PackageName,
        release.VersionCode,
        release.VersionName,
        release.MinSdk,
        urls.Build(releaseStore.RelativePath(release.FileName)),
        release.Size,
        release.Sha256,
        release.CertSha256,
        release.ReleaseNotes,
        release.PublishedAt);
}
