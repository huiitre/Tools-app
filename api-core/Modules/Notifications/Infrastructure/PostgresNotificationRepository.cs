using Dapper;
using Npgsql;
using NpgsqlTypes;
using Tools.ApiCore.Modules.Notifications.Application.Ports;

namespace Tools.ApiCore.Modules.Notifications.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port INotificationRepository.
//
// Les deux tables sont partagées avec l'API Java, qui écrit et lit les mêmes lignes tant que
// le module de notifications n'a pas migré. Le format des colonnes doit donc rester le sien.
public sealed class PostgresNotificationRepository(NpgsqlDataSource dataSource) : INotificationRepository
{
    public async Task<long> CreateAsync(
        string title,
        string body,
        string type,
        long? targetUserId,
        string? metadata)
    {
        // target_role_id et target_module_id restent nuls : le ciblage par rôle minimum n'est
        // pas persistable en l'état — l'API Java ne le stocke pas davantage, elle ne conserve
        // que les critères qui désignent une ligne précise.
        const string sql = """
            INSERT INTO tools_core.notifications (title, body, type, target_user_id, metadata)
            VALUES (@Title, @Body, @Type, @TargetUserId, @Metadata)
            RETURNING id
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<long>(new CommandDefinition(sql, new
        {
            Title = title,
            Body = body,
            Type = type,
            TargetUserId = targetUserId,
            // metadata est de type jsonb : sans ce typage explicite, Npgsql enverrait du texte.
            Metadata = metadata is null ? null : new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, Value = metadata }
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

    public async Task<bool> UserExistsAsync(long userId)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.users WHERE id = @UserId)";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { UserId = userId }));
    }
}
