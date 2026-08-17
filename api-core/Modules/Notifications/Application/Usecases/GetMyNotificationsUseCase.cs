using Microsoft.AspNetCore.Mvc;
using Tools.ApiCore.Modules.Notifications.Application;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

public sealed class GetMyNotificationsUseCase() : SecuredQuery<List<NotificationView>>
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;

    protected override async Task<List<NotificationView>> Handle()
    {
        return new NotificationView(
            1, 1, "wdfdfqsd", NotificationType.Info, "sqqsd", new DateTime(), true
        );
    }
}