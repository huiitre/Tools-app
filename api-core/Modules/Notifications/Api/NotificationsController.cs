using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tools.ApiCore.Modules.Notifications.Api;

[ApiController]
[Route("notifications")]
[AllowAnonymous]
public class NotificationsController(
    GetMyNotificationsUseCase getMyNotificationsUseCase
) : ControllerBase
{
    //* getMyNotifications GET
    [HttpGet]
    public async Task<List<NotificationView>> GetMyNotifications()
    {
        return await getMyNotificationsUseCase.Execute();
    }

    //* sendNotification POST

    //* markAsRead PATCH

    //* delete DELETE
}

public interface IActionResult<T>
{
}