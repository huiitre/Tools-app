using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Auth.Application.Ports;
using Tools.ApiCore.Modules.Common.Infrastructure;
using Tools.ApiCore.Modules.Auth.Application.Ports.Password;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du port IUserAuthProviderRepository.
public sealed class PostgresUserAuthProviderRepository(PostgresSession session) : IUserAuthProviderRepository
{
    public async Task<bool> ExistsAsync(long userId, string provider, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM tools_core.user_auth_provider
                WHERE user_id = @UserId AND provider = @Provider
            )
            """;

        return await Connection().ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { UserId = userId, Provider = provider },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(
        long userId,
        string provider,
        string providerUserId,
        string? providerEmail,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tools_core.user_auth_provider (user_id, provider, provider_user_id, provider_email)
            VALUES (@UserId, @Provider, @ProviderUserId, @ProviderEmail)
            """;

        await Connection().ExecuteAsync(new CommandDefinition(
            sql,
            new { UserId = userId, Provider = provider, ProviderUserId = providerUserId, ProviderEmail = providerEmail },
            session.Transaction,
            cancellationToken: cancellationToken));
    }

    private NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
