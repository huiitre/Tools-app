using Tools.Api.Modules.Core.AndroidApps.Application.Commands;
using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.AndroidApps.Application.Views;
using Tools.Api.Modules.Core.AndroidApps.Domain;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-3 : libellé, ordre, état actif, version recommandée, notes. Les champs absents restent inchangés.
public sealed class UpdateAndroidAppUseCase(
    UseCaseAuthorizer authorizer,
    IAndroidAppRepository repository,
    ITransactionManager transactionManager,
    IAndroidAppAssetUrlBuilder urls
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<AndroidAppView> Execute(long appId, UpdateAndroidAppCommand command)
    {
        var app = await repository.FindById(appId)
            ?? throw AppException.NotFound("ANDROID_APP_NOT_FOUND", "App introuvable.");

        var label = command.Label is null ? app.Label : AndroidApp.NormalizeLabel(command.Label);
        var recommendedReleaseId = command.RecommendedReleaseId ?? app.RecommendedReleaseId;
        var adminNotes = command.AdminNotes is null ? app.AdminNotes : AndroidApp.NormalizeAdminNotes(command.AdminNotes);

        // La base le garantit aussi (clé composite) ; vérifié ici pour un message lisible.
        if (command.RecommendedReleaseId is { } releaseId && app.Releases.All(release => release.Id != releaseId))
        {
            throw AppException.Validation(
                "ANDROID_APP_RELEASE_MISMATCH", "Cette release n'appartient pas à cette app.");
        }

        await using (var transaction = await transactionManager.BeginAsync())
        {
            await repository.UpdateApp(
                appId,
                label,
                command.DisplayOrder ?? app.DisplayOrder,
                command.IsActive ?? app.IsActive,
                recommendedReleaseId,
                adminNotes);

            await transaction.CommitAsync();
        }

        var updated = await repository.FindById(appId)
            ?? throw AppException.NotFound("ANDROID_APP_NOT_FOUND", "App introuvable.");

        return AndroidAppView.From(updated, urls);
    }
}
