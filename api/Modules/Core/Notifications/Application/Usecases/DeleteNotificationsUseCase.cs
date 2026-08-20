using Tools.Api.Modules.Core.Notifications.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;

namespace Tools.Api.Modules.Core.Notifications.Application.Usecases;

// Supprime des notifications de l'appelant.
//
// Ne disparaît que le lien vers l'utilisateur : le message source reste, les autres destinataires
// le conservent.
public sealed class DeleteNotificationsUseCase(
    UseCaseAuthorizer authorizer,
    INotificationRepository notificationRepository) : SecuredUseCase(authorizer)
{
    // `notificationIds` nul : tout est supprimé.
    public Task Execute(IReadOnlyCollection<long>? notificationIds)
    {
        return notificationRepository.DeleteAsync(CurrentUser.UserId, notificationIds);
    }
}
