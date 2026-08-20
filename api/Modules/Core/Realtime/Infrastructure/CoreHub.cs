using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Tools.Api.Modules.Core.Security.Infrastructure;

namespace Tools.Api.Modules.Core.Realtime.Infrastructure;

// Point de connexion temps réel unique du Core : chaque utilisateur connecté rejoint son
// groupe à la connexion, et tout module (notifications aujourd'hui, un autre demain) pousse
// vers ce groupe via IRealtimePublisher — jamais directement via ce Hub.
[Authorize]
public sealed class CoreHub : Hub
{
    public static string GroupName(long userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(JwtClaims.Subject);
        if (userId is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(long.Parse(userId)));
        }

        await base.OnConnectedAsync();
    }
}
