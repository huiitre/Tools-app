using Dapper;
using Tools.Api.Modules.Core.Auth.Application;
using Tools.Api.Modules.Core.Auth.Application.Ports;
using Tools.Api.Modules.Core.Auth.Domain;
using Tools.Api.Modules.Core.Common.Infrastructure;
using Tools.Api.Modules.Core.Auth.Application.Ports.Google;

namespace Tools.Api.Modules.Core.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper des opérations atomiques de rattachement d'une identité Google.
public sealed class PostgresGoogleAuthRepository(PostgresSession session) : IGoogleAuthRepository
{
    public async Task<AuthUser?> FindByGoogleProviderIdAsync(string providerUserId)
    {
        const string sql = """
            SELECT u.id AS Id, u.email AS Email, u.is_active AS IsActive, u.user_type AS UserType
            FROM tools_core.user_auth_provider provider
            INNER JOIN tools_core.users u ON u.id = provider.user_id
            WHERE provider.provider = 'GOOGLE' AND provider.provider_user_id = @ProviderUserId
            """;
        return await Connection().QuerySingleOrDefaultAsync<AuthUser>(
            new CommandDefinition(sql, new { ProviderUserId = providerUserId }, session.Transaction));
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.users WHERE email = @Email)";
        return await Connection().ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Email = email }, session.Transaction));
    }

    public async Task<AuthUser> CreateGoogleUserAsync(GoogleIdentity identity)
    {
        // email_verified_at à now() : Google a déjà confirmé l'adresse, et sans ça le compte
        // est supprimé sous 30 min par EmailVerificationCleanupService (qui le traite comme une
        // inscription classique abandonnée, faute de jeton de vérification).
        const string userSql = """
            INSERT INTO tools_core.users (name, email, is_active, user_type, avatar_source, email_verified_at)
            VALUES (@Name, @Email, true, 'HUMAN', 'GOOGLE', now())
            RETURNING id AS Id, email AS Email, is_active AS IsActive, user_type AS UserType
            """;
        var connection = Connection();
        var user = await connection.QuerySingleAsync<AuthUser>(
            new CommandDefinition(userSql, identity, session.Transaction));

        const string providerSql = """
            INSERT INTO tools_core.user_auth_provider (user_id, provider, provider_user_id, provider_email, provider_avatar_url)
            VALUES (@UserId, 'GOOGLE', @ProviderUserId, @Email, @PictureUrl)
            """;
        await connection.ExecuteAsync(new CommandDefinition(
            providerSql,
            new { UserId = user.Id, identity.ProviderUserId, identity.Email, identity.PictureUrl },
            session.Transaction));

        const string roleSql = """
            INSERT INTO tools_core.user_role (user_id, role_id)
            SELECT @UserId, id FROM tools_core.role WHERE code = 'USER'
            """;
        var insertedRoles = await connection.ExecuteAsync(new CommandDefinition(
            roleSql, new { UserId = user.Id }, session.Transaction));
        if (insertedRoles != 1)
        {
            throw new InvalidOperationException("Le rôle USER est introuvable.");
        }

        return user;
    }

    public async Task UpdateGoogleAvatarAsync(long userId, string pictureUrl)
    {
        const string sql = """
            UPDATE tools_core.user_auth_provider
            SET provider_avatar_url = @PictureUrl
            WHERE user_id = @UserId AND provider = 'GOOGLE'
            """;
        await Connection().ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, PictureUrl = pictureUrl }, session.Transaction));
    }

    private Npgsql.NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
