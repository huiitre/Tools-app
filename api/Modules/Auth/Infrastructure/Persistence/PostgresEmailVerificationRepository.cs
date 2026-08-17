using Dapper;
using Npgsql;
using Tools.Api.Modules.Auth.Application.Ports.Registration;
using Tools.Api.Modules.Common.Infrastructure;

namespace Tools.Api.Modules.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du port IEmailVerificationRepository.
//
// Les écritures liées à une requête HTTP passent par la transaction ouverte par le use case.
// Le nettoyage planifié, lui, s'exécute hors transaction : il utilise le pool directement.
public sealed class PostgresEmailVerificationRepository(
    PostgresSession session,
    NpgsqlDataSource dataSource) : IEmailVerificationRepository
{
    public async Task SaveAsync(long userId, string token, DateTime expiresAt)
    {
        const string sql = """
            INSERT INTO tools_core.user_email_verification (user_id, token, expires_at)
            VALUES (@UserId, @Token, @ExpiresAt)
            """;

        await Connection().ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, Token = token, ExpiresAt = expiresAt }, session.Transaction));
    }

    public async Task<long?> FindUserIdByValidTokenAsync(string token, DateTime now)
    {
        const string sql = """
            SELECT user_id
            FROM tools_core.user_email_verification
            WHERE token = @Token AND expires_at > @Now
            """;

        return await Connection().QuerySingleOrDefaultAsync<long?>(
            new CommandDefinition(sql, new { Token = token, Now = now }, session.Transaction));
    }

    public async Task DeleteByUserIdAsync(long userId)
    {
        const string sql = "DELETE FROM tools_core.user_email_verification WHERE user_id = @UserId";

        await Connection().ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId }, session.Transaction));
    }

    public async Task<int> DeleteExpiredAsync(DateTime now)
    {
        const string sql = "DELETE FROM tools_core.user_email_verification WHERE expires_at <= @Now";

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { Now = now }));
    }

    public async Task<int> DeleteAbandonedRegistrationsAsync(DateTime now)
    {
        // Le critère est email_verified_at IS NULL, jamais is_active : un compte suspendu par
        // un administrateur garde une adresse confirmée et n'entre donc pas dans ce filet.
        //
        // La suppression cascade sur user_credentials, user_auth_provider, user_role et
        // user_email_verification.
        const string sql = """
            DELETE FROM tools_core.users u
            WHERE u.email_verified_at IS NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM tools_core.user_email_verification v
                  WHERE v.user_id = u.id AND v.expires_at > @Now
              )
            """;

        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { Now = now }));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
