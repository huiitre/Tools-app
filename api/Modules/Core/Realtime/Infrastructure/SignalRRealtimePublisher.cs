using Microsoft.AspNetCore.SignalR;
using Tools.Api.Modules.Core.Realtime.Application.Ports;

namespace Tools.Api.Modules.Core.Realtime.Infrastructure;

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
