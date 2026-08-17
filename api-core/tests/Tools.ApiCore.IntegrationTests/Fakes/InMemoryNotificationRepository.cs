using Tools.ApiCore.Modules.Notifications.Application.Ports;

namespace Tools.ApiCore.IntegrationTests.Fakes;

// Conserve les notifications enregistrées pour que le test les inspecte.
//
// Les destinataires sont fixés : la résolution par rôle relève du SQL, pas du comportement
// applicatif. Ce que ces tests vérifient, c'est qu'une notification part bien, avec le bon
// titre et le bon ciblage.
public sealed class InMemoryNotificationRepository : INotificationRepository
{
    public const long AdminUserId = 99;
    public const long ModuleMemberUserId = 77;

    private readonly List<RecordedNotification> notifications = [];

    public IReadOnlyList<RecordedNotification> Notifications => notifications;

    public IReadOnlyList<string> RoleCodesAsked { get; private set; } = [];

    public long? ModuleIdAsked { get; private set; }

    public void Clear()
    {
        notifications.Clear();
        RoleCodesAsked = [];
        ModuleIdAsked = null;
    }

    public Task<long> CreateAsync(
        string title, string body, string type, long? targetUserId, long? targetModuleId, string? metadata)
    {
        notifications.Add(new RecordedNotification(title, body, type, targetUserId, targetModuleId, metadata));
        return Task.FromResult((long)notifications.Count);
    }

    public Task<IReadOnlyList<long>> FindRecipientsByRoleCodesAsync(IReadOnlyCollection<string> roleCodes)
    {
        RoleCodesAsked = roleCodes.ToList();
        return Task.FromResult<IReadOnlyList<long>>([AdminUserId]);
    }

    public Task<IReadOnlyList<long>> FindRecipientsByModuleIdAsync(long moduleId)
    {
        ModuleIdAsked = moduleId;
        return Task.FromResult<IReadOnlyList<long>>([ModuleMemberUserId]);
    }

    public Task AddRecipientsAsync(long notificationId, IReadOnlyCollection<long> userIds) => Task.CompletedTask;

    public Task<bool> UserExistsAsync(long userId) => Task.FromResult(true);
}

public sealed record RecordedNotification(
    string Title,
    string Body,
    string Type,
    long? TargetUserId,
    long? TargetModuleId,
    string? Metadata);
