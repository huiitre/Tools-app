using Tools.ApiCore.Modules.Notifications.Application.Ports;
using Tools.ApiCore.Modules.Notifications.Application.Views;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;

namespace Tools.ApiCore.Modules.Notifications.Application.Usecases;

// Notifications de l'appelant.
//
// Ni `RequiredRole` ni `RequiredModule` ne sont surchargés : le contrôle par défaut suffit, soit
// un compte authentifié portant au moins READ_ONLY. Lire ses propres notifications n'exige rien
// de plus — c'est le rôle qu'exigeait déjà l'API Java.
public sealed class GetMyNotificationsUseCase(
    UseCaseAuthorizer authorizer,
    INotificationRepository notificationRepository) : SecuredUseCase(authorizer)
{
    // Le destinataire n'est pas un argument : c'est l'appelant validé, hors de portée du client.
    public Task<IReadOnlyList<NotificationView>> Execute()
    {
        return notificationRepository.FindActiveForUserAsync(CurrentUser.UserId);
    }
}
