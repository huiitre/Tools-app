using Microsoft.AspNetCore.SignalR;
using Tools.ApiCore.Modules.Realtime.Application.Ports;

namespace Tools.ApiCore.Modules.Realtime.Infrastructure;

public sealed class SignalRRealtimePublisher(IHubContext<CoreHub> hubContext) : IRealtimePublisher
{
    public Task PublishAsync(IReadOnlyCollection<long> userIds, string eventType, object payload)
    {
        if (userIds.Count == 0)
        {
            return Task.CompletedTask;
        }

        var groups = userIds.Select(CoreHub.GroupName).ToArray();
        return hubContext.Clients.Groups(groups).SendAsync(eventType, payload);
    }
}
