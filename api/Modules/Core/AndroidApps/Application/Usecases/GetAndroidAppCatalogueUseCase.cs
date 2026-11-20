using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Application.Views;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-6 : lu par les TV, sans compte. Volontairement non sécurisé : la TV n'a pas d'identité, une
// clé embarquée dans l'APK serait extractible, et le catalogue ne contient aucun secret (des
// versions et des URL d'APK déjà publiques).
public sealed class GetAndroidAppCatalogueUseCase(
    IAndroidAppRepository repository,
    IAndroidAppAssetUrlBuilder urls)
{
    public async Task<AndroidAppCatalogueView> Execute()
    {
        var apps = await repository.FindCatalogue();

        return new AndroidAppCatalogueView(apps
            .Where(app => app.RecommendedRelease is not null)
            .Select(app =>
            {
                var release = app.RecommendedRelease!;

                return new AndroidAppCatalogueAppView(
                    app.PackageName,
                    app.Label,
                    new AndroidAppCatalogueReleaseView(
                        release.VersionCode,
                        release.VersionName,
                        release.MinSdk,
                        urls.Build(release.FilePath),
                        release.FileSize,
                        Convert.ToHexStringLower(release.FileSha256),
                        Convert.ToHexStringLower(release.CertSha256),
                        release.ReleaseNotes));
            })
            .ToList());
    }
}
