using Tools.Api.Modules.Notifications.Application.Ports;
using Tools.Api.Modules.Notifications.Application.Views;

namespace Tools.Api.IntegrationTests.Fakes;

// Conserve les notifications enregistrées pour que le test les inspecte.
//
// Les destinataires sont fixés : la résolution par rôle relève du SQL, pas du comportement
// applicatif. Ce que ces tests vérifient, c'est qu'une notification part bien, avec le bon
// titre et le bon ciblage.
//
// La lecture, elle, tient un état par destinataire : le point à vérifier n'est pas le SQL mais
// qu'un appelant ne voie et ne touche que ses propres lignes.
public sealed class InMemoryNotificationRepository : INotificationRepository
{
    public const long AdminUserId = 99;
    public const long ModuleMemberUserId = 77;

    private readonly List<RecordedNotification> notifications = [];
    private readonly Dictionary<long, List<RecipientLine>> linesByUser = [];

    public IReadOnlyList<RecordedNotification> Notifications => notifications;

    public IReadOnlyList<string> RoleCodesAsked { get; private set; } = [];

    public long? ModuleIdAsked { get; private set; }

    public void Clear()
    {
        notifications.Clear();
        linesByUser.Clear();
        RoleCodesAsked = [];
        ModuleIdAsked = null;
    }

    // Prépare l'historique d'un destinataire sans passer par un envoi.
    public void GiveTo(long userId, long notificationId, string title, bool read = false)
    {
        notifications.Add(new RecordedNotification(title, "corps", "INFO", userId, null, null));
        Lines(userId).Add(new RecipientLine(notificationId, title, read));
    }

    public IReadOnlyList<long> UnreadIdsOf(long userId) =>
        Lines(userId).Where(line => !line.Read).Select(line => line.NotificationId).ToList();

    public IReadOnlyList<long> IdsOf(long userId) =>
        Lines(userId).Select(line => line.NotificationId).ToList();

    public Task<IReadOnlyList<NotificationView>> FindActiveForUserAsync(long userId)
    {
        IReadOnlyList<NotificationView> views = Lines(userId)
            .OrderByDescending(line => line.NotificationId)
            .Select(line => new NotificationView(
                line.NotificationId,
                line.Title,
                "corps",
                "INFO",
                null,
                new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc),
                line.Read))
            .ToList();

        return Task.FromResult(views);
    }

    public Task MarkAsReadAsync(long userId, IReadOnlyCollection<long>? notificationIds)
    {
        var lines = Lines(userId);

        for (var index = 0; index < lines.Count; index++)
        {
            if (notificationIds is null
                || notificationIds.Count == 0
                || notificationIds.Contains(lines[index].NotificationId))
            {
                lines[index] = lines[index] with { Read = true };
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(long userId, IReadOnlyCollection<long>? notificationIds)
    {
        var lines = Lines(userId);

        if (notificationIds is null || notificationIds.Count == 0)
        {
            lines.Clear();
        }
        else
        {
            lines.RemoveAll(line => notificationIds.Contains(line.NotificationId));
        }

        return Task.CompletedTask;
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

    private List<RecipientLine> Lines(long userId)
    {
        if (!linesByUser.TryGetValue(userId, out var lines))
        {
            lines = [];
            linesByUser[userId] = lines;
        }

        return lines;
    }

    private sealed record RecipientLine(long NotificationId, string Title, bool Read);
}

public sealed record RecordedNotification(
    string Title,
    string Body,
    string Type,
    long? TargetUserId,
    long? TargetModuleId,
    string? Metadata);
