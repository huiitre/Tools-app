using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Common.Infrastructure;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du port IPasswordResetRepository.
// Les écritures participent à la transaction ouverte par le use case.
public sealed class PostgresPasswordResetRepository(
    PostgresSession session,
    NpgsqlDataSource dataSource) : IPasswordResetRepository
{
    public async Task SaveAsync(long userId, string token, DateTime expiresAt, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tools_core.user_password_reset (user_id, token, expires_at)
            VALUES (@UserId, @Token, @ExpiresAt)
            """;

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, Token = token, ExpiresAt = expiresAt },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<long?> FindUserIdByValidTokenAsync(string token, DateTime now, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT user_id
            FROM tools_core.user_password_reset
            WHERE token = @Token AND expires_at > @Now
            """;

        return await Connection().QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            sql,
            new { Token = token, Now = now },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task DeleteByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM tools_core.user_password_reset WHERE user_id = @UserId";

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM tools_core.user_password_reset WHERE expires_at <= @Now";

        // Le nettoyage planifié tourne hors requête HTTP : il n'a pas de transaction ouverte.
        if (session.Connection is null)
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return await connection.ExecuteAsync(new CommandDefinition(
                sql, new { Now = now }, cancellationToken: cancellationToken));
        }

        return await Connection().ExecuteAsync(new CommandDefinition(
            sql, new { Now = now }, session.Transaction, cancellationToken: cancellationToken));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
