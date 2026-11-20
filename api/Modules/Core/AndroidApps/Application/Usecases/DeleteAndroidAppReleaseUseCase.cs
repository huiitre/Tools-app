using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-4 : la ligne d'abord, le fichier ensuite. Un fichier qui résiste reste orphelin sur le disque
// (journalisé) plutôt qu'une ligne pointant vers un fichier disparu.
public sealed class DeleteAndroidAppReleaseUseCase(
    UseCaseAuthorizer authorizer,
    IAndroidAppRepository repository,
    IAndroidAppFileStore fileStore,
    ITransactionManager transactionManager,
    ILogger<DeleteAndroidAppReleaseUseCase> logger
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(long appId, long releaseId)
    {
        var app = await repository.FindById(appId)
            ?? throw AppException.NotFound("ANDROID_APP_NOT_FOUND", "App introuvable.");

        var release = app.Releases.FirstOrDefault(candidate => candidate.Id == releaseId)
            ?? throw AppException.NotFound("ANDROID_APP_RELEASE_NOT_FOUND", "Release introuvable.");

        if (app.RecommendedReleaseId == releaseId)
        {
            throw AppException.Conflict(
                "ANDROID_APP_RELEASE_RECOMMENDED",
                "Cette version est celle proposée sur le store : proposez-en une autre avant de la supprimer.");
        }

        await using (var transaction = await transactionManager.BeginAsync())
        {
            await repository.DeleteRelease(releaseId);
            await transaction.CommitAsync();
        }

        try
        {
            fileStore.Delete(release.FilePath);
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "APK supprimé de la base mais pas du disque : {FilePath}", release.FilePath);
        }
    }
}
