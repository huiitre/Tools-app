using Dapper;
using Npgsql;
using Tools.Api.Modules.Auth.Application.Ports;
using Tools.Api.Modules.Common.Infrastructure;
using Tools.Api.Modules.Auth.Application.Ports.Password;

namespace Tools.Api.Modules.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du port IPasswordResetRepository.
// Les écritures participent à la transaction ouverte par le use case.
public sealed class PostgresPasswordResetRepository(
    PostgresSession session,
    NpgsqlDataSource dataSource) : IPasswordResetRepository
{
    public async Task SaveAsync(long userId, string token, DateTime expiresAt)
    {
        const string sql = """
            INSERT INTO tools_core.user_password_reset (user_id, token, expires_at)
            VALUES (@UserId, @Token, @ExpiresAt)
            """;

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, Token = token, ExpiresAt = expiresAt },
            session.Transaction));
    }

    public async Task<long?> FindUserIdByValidTokenAsync(string token, DateTime now)
    {
        const string sql = """
            SELECT user_id
            FROM tools_core.user_password_reset
            WHERE token = @Token AND expires_at > @Now
            """;

        return await Connection().QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            sql,
            new { Token = token, Now = now },
            session.Transaction));
    }

    public async Task DeleteByUserIdAsync(long userId)
    {
        const string sql = "DELETE FROM tools_core.user_password_reset WHERE user_id = @UserId";

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId },
            session.Transaction));
    }

    public async Task<int> DeleteExpiredAsync(DateTime now)
    {
        const string sql = "DELETE FROM tools_core.user_password_reset WHERE expires_at <= @Now";

        // Le nettoyage planifié tourne hors requête HTTP : il n'a pas de transaction ouverte.
        if (session.Connection is null)
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            return await connection.ExecuteAsync(new CommandDefinition(
                sql, new { Now = now }));
        }

        return await Connection().ExecuteAsync(new CommandDefinition(
            sql, new { Now = now }, session.Transaction));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
