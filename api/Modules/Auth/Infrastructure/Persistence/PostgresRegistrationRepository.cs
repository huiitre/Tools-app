using Dapper;
using Tools.ApiCore.Modules.Auth.Application.Ports.Registration;
using Tools.ApiCore.Modules.Common.Infrastructure;

namespace Tools.ApiCore.Modules.Auth.Infrastructure.Persistence;

// Adaptateur PostgreSQL/Dapper du port IRegistrationRepository.
//
// Ces écritures participent toutes à la transaction ouverte par le use case : créer un
// utilisateur sans ses credentials, son provider ou son rôle laisserait un compte inutilisable.
public sealed class PostgresRegistrationRepository(PostgresSession session) : IRegistrationRepository
{
    public async Task<RegisteredAccount?> FindAccountByEmailAsync(string email)
    {
        const string sql = """
            SELECT id AS Id, email AS Email, is_active AS IsActive, email_verified_at AS EmailVerifiedAt
            FROM tools_core.users
            WHERE email = @Email
            """;

        return await Connection().QuerySingleOrDefaultAsync<RegisteredAccount>(
            new CommandDefinition(sql, new { Email = email }, session.Transaction));
    }

    public async Task<long> CreatePendingUserAsync(string name, string email, string passwordHash)
    {
        // is_active reste false et email_verified_at null : la confirmation fera les deux.
        const string userSql = """
            INSERT INTO tools_core.users (name, email, is_active, user_type, avatar_source)
            VALUES (@Name, @Email, false, 'HUMAN', 'PASSWORD')
            RETURNING id
            """;

        var connection = Connection();
        var userId = await connection.QuerySingleAsync<long>(
            new CommandDefinition(userSql, new { Name = name, Email = email }, session.Transaction));

        const string credentialsSql = """
            INSERT INTO tools_core.user_credentials (user_id, password_hash)
            VALUES (@UserId, @PasswordHash)
            """;
        await connection.ExecuteAsync(
            new CommandDefinition(credentialsSql, new { UserId = userId, PasswordHash = passwordHash }, session.Transaction));

        // provider_user_id vaut l'email, même convention qu'à la création d'un mot de passe.
        const string providerSql = """
            INSERT INTO tools_core.user_auth_provider (user_id, provider, provider_user_id, provider_email)
            VALUES (@UserId, 'PASSWORD', @Email, @Email)
            """;
        await connection.ExecuteAsync(
            new CommandDefinition(providerSql, new { UserId = userId, Email = email }, session.Transaction));

        const string roleSql = """
            INSERT INTO tools_core.user_role (user_id, role_id)
            SELECT @UserId, id FROM tools_core.role WHERE code = 'USER'
            """;
        var insertedRoles = await connection.ExecuteAsync(
            new CommandDefinition(roleSql, new { UserId = userId }, session.Transaction));

        if (insertedRoles != 1)
        {
            // Sans rôle, le compte serait créé mais incapable d'agir : la transaction est annulée.
            throw new InvalidOperationException("Le rôle USER est introuvable.");
        }

        return userId;
    }

    public async Task ReplacePendingPasswordAsync(long userId, string passwordHash)
    {
        const string sql = """
            UPDATE tools_core.user_credentials
            SET password_hash = @PasswordHash
            WHERE user_id = @UserId
            """;

        var updated = await Connection().ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, PasswordHash = passwordHash }, session.Transaction));

        if (updated == 0)
        {
            // Compte créé par un autre chemin que le mot de passe : on complète au lieu d'échouer.
            const string insertSql = """
                INSERT INTO tools_core.user_credentials (user_id, password_hash)
                VALUES (@UserId, @PasswordHash)
                """;
            await Connection().ExecuteAsync(
                new CommandDefinition(insertSql, new { UserId = userId, PasswordHash = passwordHash }, session.Transaction));
        }
    }

    public async Task MarkEmailVerifiedAsync(long userId, DateTime verifiedAt)
    {
        // is_active passe à true à la confirmation ; email_verified_at garde la trace du fait
        // que l'adresse a été confirmée, indépendamment d'une suspension ultérieure.
        const string sql = """
            UPDATE tools_core.users
            SET is_active = true, email_verified_at = @VerifiedAt, updated_at = now()
            WHERE id = @UserId
            """;

        await Connection().ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, VerifiedAt = verifiedAt }, session.Transaction));
    }

    public async Task<string?> FindEmailByIdAsync(long userId)
    {
        const string sql = "SELECT email FROM tools_core.users WHERE id = @UserId";

        return await Connection().QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(sql, new { UserId = userId }, session.Transaction));
    }

    private Npgsql.NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
