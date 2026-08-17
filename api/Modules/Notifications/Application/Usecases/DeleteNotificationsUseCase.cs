using Tools.Api.Modules.Notifications.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Application.Usecases;

namespace Tools.Api.Modules.Notifications.Application.Usecases;

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
