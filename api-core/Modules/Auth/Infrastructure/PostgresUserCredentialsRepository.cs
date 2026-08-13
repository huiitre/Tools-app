using Dapper;
using Npgsql;

// Adaptateur PostgreSQL/Dapper du port IUserCredentialsRepository.
public sealed class PostgresUserCredentialsRepository(PostgresSession session) : IUserCredentialsRepository
{
    public async Task<bool> ExistsAsync(long userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.user_credentials WHERE user_id = @UserId)";

        return await Connection().ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { UserId = userId }, session.Transaction, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(long userId, string passwordHash, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tools_core.user_credentials (user_id, password_hash)
            VALUES (@UserId, @PasswordHash)
            """;

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, PasswordHash = passwordHash },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<int> UpdatePasswordAsync(long userId, string passwordHash, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tools_core.user_credentials
            SET password_hash = @PasswordHash
            WHERE user_id = @UserId
            """;

        return await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, PasswordHash = passwordHash },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
