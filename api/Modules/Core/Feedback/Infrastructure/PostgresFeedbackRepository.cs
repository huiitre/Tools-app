using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.Feedback.Application.Dto;
using Tools.Api.Modules.Core.Feedback.Application.Ports;

namespace Tools.Api.Modules.Core.Feedback.Infrastructure;

// Adaptateur PostgreSQL/Dapper du port IFeedbackRepository.
//
// Table héritée de l'API Java (tools_core.feedbacks) : la migration du module déplace le code,
// pas les données.
public sealed class PostgresFeedbackRepository(NpgsqlDataSource dataSource) : IFeedbackRepository
{
    public async Task Save(long userId, string message)
    {
        const string sql = """
            INSERT INTO tools_core.feedbacks (user_id, message)
            VALUES (@UserId, @Message)
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, Message = message }));
    }

    public async Task<List<FeedbackDto>> FindAllSortedByDateDesc()
    {
        // Colonnes aliasées sur les propriétés du DTO : Dapper apparie par nom.
        const string sql = """
            SELECT f.id         AS "Id",
                   f.user_id    AS "UserId",
                   u.name       AS "UserName",
                   f.message    AS "Message",
                   f.is_read    AS "IsRead",
                   f.created_at AS "CreatedAt"
            FROM tools_core.feedbacks f
            INNER JOIN tools_core.users u ON u.id = f.user_id
            ORDER BY f.created_at DESC
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        var feedbacks = await connection.QueryAsync<FeedbackDto>(new CommandDefinition(sql));
        return feedbacks.ToList();
    }

    public async Task DeleteByIds(List<long> ids)
    {
        if (ids.Count == 0) return;

        const string sql = "DELETE FROM tools_core.feedbacks WHERE id = ANY(@Ids)";

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Ids = ids.ToArray() }));
    }

    public async Task UpdateReadStatus(List<long> ids, bool isRead)
    {
        if (ids.Count == 0) return;

        const string sql = """
            UPDATE tools_core.feedbacks
            SET is_read = @IsRead
            WHERE id = ANY(@Ids)
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Ids = ids.ToArray(), IsRead = isRead }));
    }
}
