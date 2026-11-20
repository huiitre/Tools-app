using Microsoft.Extensions.Options;
using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Huihuistore.Application.Ports;
using Tools.Api.Modules.Core.Huihuistore.Application.Views;
using Tools.Api.Modules.Core.Huihuistore.Domain;
using Tools.Api.Modules.Core.Huihuistore.Infrastructure;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Huihuistore.Application.Usecases;

// Appelé par la CI de huihuistore avec sa clé d'API (scope huihuistore:publish), ou à la main par un
// admin. L'APK est reçu hors de tools_assets, lu, contrôlé, puis publié : les appareils le voient
// dès que version.json est réécrit.
public sealed class PublishHuihuistoreReleaseUseCase(
    UseCaseAuthorizer authorizer,
    IHuihuistoreReleaseStore releaseStore,
    IApkInspector inspector,
    IOptions<HuihuistoreOptions> options,
    IAndroidAppAssetUrlBuilder urls
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;
    protected override ApiKeyScope? RequiredScope => ApiKeyScope.HuihuistorePublish;

    private const int ReleaseNotesMaxLength = 5000;

    public async Task<HuihuistoreUpdateView> Execute(Stream content, string? releaseNotes, CancellationToken cancellationToken)
    {
        var notes = string.IsNullOrWhiteSpace(releaseNotes) ? null : releaseNotes.Trim();

        if (notes?.Length > ReleaseNotesMaxLength)
        {
            throw AppException.Validation(
                "HUIHUISTORE_RELEASE_NOTES_TOO_LONG",
                $"Les notes ne doivent pas dépasser {ReleaseNotesMaxLength} caractères.");
        }

        var stagingPath = releaseStore.NewStagingPath();

        try
        {
            await using (var file = new FileStream(stagingPath, FileMode.CreateNew, FileAccess.Write))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            var apk = await inspector.Inspect(stagingPath, cancellationToken);
            var current = await releaseStore.FindCurrent(cancellationToken);

            HuihuistorePublishRules.Validate(apk, options.Value.PackageName, options.Value.CertificateSha256, current);

            var release = new HuihuistoreRelease(
                apk.PackageName,
                apk.VersionCode,
                apk.VersionName,
                apk.MinSdk,
                $"{apk.VersionCode}.apk",
                apk.FileSize,
                Convert.ToHexStringLower(apk.FileSha256),
                Convert.ToHexStringLower(apk.CertSha256),
                notes,
                DateTimeOffset.UtcNow);

            await releaseStore.Publish(stagingPath, release, cancellationToken);

            return GetHuihuistoreUpdateUseCase.ToView(release, releaseStore, urls);
        }
        finally
        {
            if (File.Exists(stagingPath))
            {
                File.Delete(stagingPath);
            }
        }
    }
}
