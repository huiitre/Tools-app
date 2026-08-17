namespace Tools.Api.Modules.Realtime.Application.Ports;

public interface IRealtimePublisher
{
    Task PublishAsync(IReadOnlyCollection<long> userIds, string eventType, object payload);
}
