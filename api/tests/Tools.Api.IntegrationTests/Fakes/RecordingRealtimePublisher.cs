using Tools.Api.Modules.Core.Realtime.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Remplace le push SignalR réel : la publication reçue est conservée pour être inspectée par le test.
public sealed class RecordingRealtimePublisher : IRealtimePublisher
{
    public sealed record RecordedPublish(IReadOnlyCollection<long> UserIds, string EventType, object Payload);

    public RecordedPublish? LastPublish { get; private set; }

    public Task PublishAsync(IReadOnlyCollection<long> userIds, string eventType, object payload)
    {
        LastPublish = new RecordedPublish(userIds, eventType, payload);
        return Task.CompletedTask;
    }

    public void Clear() => LastPublish = null;
}
