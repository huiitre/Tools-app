using Dapper;
using Npgsql;
using Tools.Api.Modules.Notifications.Application.Ports;
using Tools.Api.Modules.Notifications.Application.Views;

namespace Tools.Api.Modules.Notifications.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port INotificationRepository.
//
// Les deux tables restent celles de l'API Java (`tools_core.notifications` et
// `tools_core.user_notifications`) : la migration du module a déplacé le code, pas les données.
// Le format des colonnes est donc le sien, et les requêtes ci-dessous reproduisent les siennes.
public sealed class PostgresNotificationRepository(NpgsqlDataSource dataSource) : INotificationRepository
{
    public async Task<IReadOnlyList<NotificationView>> FindActiveForUserAsync(long userId)
    {
        // Les colonnes sont aliasées sur les propriétés de la vue : Dapper apparie par nom, et
        // `is_read` ne correspondrait pas à `Read`.
        const string sql = """
            SELECT n.id         AS "Id",
                   n.title      AS "Title",
                   n.body       AS "Body",
                   n.type       AS "Type",
                   n.metadata   AS "Metadata",
                   n.created_at AS "CreatedAt",
                   un.is_read   AS "Read"
            FROM tools_core.notifications n
            INNER JOIN tools_core.user_notifications un ON un.notification_id = n.id
            WHERE un.user_id = @UserId
            ORDER BY n.created_at DESC
            LIMIT 50
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var notifications = await connection.QueryAsync<NotificationView>(
            new CommandDefinition(sql, new { UserId = userId }));
        return notifications.ToList();
    }

    public async Task MarkAsReadAsync(long userId, IReadOnlyCollection<long>? notificationIds)
    {
        // Sans identifiants, tout ce qui n'est pas lu l'est. `read_at` porte l'horodatage du
        // marquage, jamais réécrit sur une ligne déjà lue — d'où le filtre sur `is_read`.
        const string markAllSql = """
            UPDATE tools_core.user_notifications
            SET is_read = TRUE, read_at = now()
            WHERE user_id = @UserId AND is_read = FALSE
            """;

        const string markSomeSql = """
            UPDATE tools_core.user_notifications
            SET is_read = TRUE, read_at = now()
            WHERE user_id = @UserId AND notification_id = ANY(@NotificationIds)
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(
            notificationIds is null || notificationIds.Count == 0 ? markAllSql : markSomeSql,
            new { UserId = userId, NotificationIds = notificationIds?.ToArray() }));
    }

    public async Task DeleteAsync(long userId, IReadOnlyCollection<long>? notificationIds)
    {
        // Le filtre sur `user_id` est la seule protection contre la suppression des notifications
        // d'autrui : les identifiants viennent du client et ne sont vérifiés nulle part ailleurs.
        const string deleteAllSql = """
            DELETE FROM tools_core.user_notifications
            WHERE user_id = @UserId
            """;

        const string deleteSomeSql = """
            DELETE FROM tools_core.user_notifications
            WHERE user_id = @UserId AND notification_id = ANY(@NotificationIds)
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(
            notificationIds is null || notificationIds.Count == 0 ? deleteAllSql : deleteSomeSql,
            new { UserId = userId, NotificationIds = notificationIds?.ToArray() }));
    }

    public async Task<long> CreateAsync(
        string title,
        string body,
        string type,
        long? targetUserId,
        long? targetModuleId,
        string? metadata)
    {
        // ::jsonb caste le paramètre texte côté Postgres — Dapper ne sait pas binder un
        // NpgsqlParameter typé directement dans un objet anonyme.
        const string sql = """
            INSERT INTO tools_core.notifications (title, body, type, target_user_id, target_module_id, metadata)
            VALUES (@Title, @Body, @Type, @TargetUserId, @TargetModuleId, @Metadata::jsonb)
            RETURNING id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<long>(new CommandDefinition(sql, new
        {
            Title = title,
            Body = body,
            Type = type,
            TargetUserId = targetUserId,
            TargetModuleId = targetModuleId,
            Metadata = metadata
        }));
    }

    public async Task<IReadOnlyList<long>> FindRecipientsByRoleCodesAsync(IReadOnlyCollection<string> roleCodes)
    {
        // Les comptes TECH sont exclus de tout envoi, exactement comme dans l'API Java. Un
        // utilisateur pouvant cumuler les rôles, l'exclusion porte sur le compte entier.
        const string sql = """
            SELECT DISTINCT ur.user_id
            FROM tools_core.user_role ur
            INNER JOIN tools_core.role r ON r.id = ur.role_id
            INNER JOIN tools_core.users u ON u.id = ur.user_id
            WHERE r.code = ANY(@RoleCodes)
              AND u.is_active
              AND NOT EXISTS (
                  SELECT 1
                  FROM tools_core.user_role tech_role
                  INNER JOIN tools_core.role tech ON tech.id = tech_role.role_id
                  WHERE tech_role.user_id = ur.user_id AND tech.code = 'TECH'
              )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var recipients = await connection.QueryAsync<long>(
            new CommandDefinition(sql, new { RoleCodes = roleCodes.ToArray() }));
        return recipients.ToList();
    }

    public async Task AddRecipientsAsync(long notificationId, IReadOnlyCollection<long> userIds)
    {
        const string sql = """
            INSERT INTO tools_core.user_notifications (user_id, notification_id)
            SELECT unnest(@UserIds), @NotificationId
            ON CONFLICT (user_id, notification_id) DO NOTHING
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserIds = userIds.ToArray(),
            NotificationId = notificationId
        }));
    }

    public async Task<IReadOnlyList<long>> FindRecipientsByModuleIdAsync(long moduleId)
    {
        const string sql = """
            SELECT DISTINCT umr.user_id
            FROM tools_core.user_module_role umr
            INNER JOIN tools_core.users u ON u.id = umr.user_id
            WHERE umr.module_id = @ModuleId
              AND u.is_active
              AND NOT EXISTS (
                  SELECT 1
                  FROM tools_core.user_role tech_role
                  INNER JOIN tools_core.role tech ON tech.id = tech_role.role_id
                  WHERE tech_role.user_id = umr.user_id AND tech.code = 'TECH'
              )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var recipients = await connection.QueryAsync<long>(
            new CommandDefinition(sql, new { ModuleId = moduleId }));
        return recipients.ToList();
    }

    public async Task<bool> UserExistsAsync(long userId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.users WHERE id = @UserId)";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId }));
    }
}
