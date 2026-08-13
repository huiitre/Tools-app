using Dapper;

// Adaptateur PostgreSQL/Dapper des opérations atomiques de rattachement d'une identité Google.
public sealed class PostgresGoogleAuthRepository(PostgresSession session) : IGoogleAuthRepository
{
    public async Task<AuthUser?> FindByGoogleProviderIdAsync(string providerUserId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT u.id AS Id, u.email AS Email, u.is_active AS IsActive, u.user_type AS UserType
            FROM tools_core.user_auth_provider provider
            INNER JOIN tools_core.users u ON u.id = provider.user_id
            WHERE provider.provider = 'GOOGLE' AND provider.provider_user_id = @ProviderUserId
            """;
        return await Connection().QuerySingleOrDefaultAsync<AuthUser>(
            new CommandDefinition(sql, new { ProviderUserId = providerUserId }, session.Transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM tools_core.users WHERE email = @Email)";
        return await Connection().ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Email = email }, session.Transaction, cancellationToken: cancellationToken));
    }

    public async Task<AuthUser> CreateGoogleUserAsync(GoogleIdentity identity, CancellationToken cancellationToken)
    {
        const string userSql = """
            INSERT INTO tools_core.users (name, email, is_active, user_type, avatar_source)
            VALUES (@Name, @Email, true, 'HUMAN', 'GOOGLE')
            RETURNING id AS Id, email AS Email, is_active AS IsActive, user_type AS UserType
            """;
        var connection = Connection();
        var user = await connection.QuerySingleAsync<AuthUser>(
            new CommandDefinition(userSql, identity, session.Transaction, cancellationToken: cancellationToken));

        const string providerSql = """
            INSERT INTO tools_core.user_auth_provider (user_id, provider, provider_user_id, provider_email, provider_avatar_url)
            VALUES (@UserId, 'GOOGLE', @ProviderUserId, @Email, @PictureUrl)
            """;
        await connection.ExecuteAsync(new CommandDefinition(
            providerSql,
            new { UserId = user.Id, identity.ProviderUserId, identity.Email, identity.PictureUrl },
            session.Transaction,
            cancellationToken: cancellationToken));

        const string roleSql = """
            INSERT INTO tools_core.user_role (user_id, role_id)
            SELECT @UserId, id FROM tools_core.role WHERE code = 'USER'
            """;
        var insertedRoles = await connection.ExecuteAsync(new CommandDefinition(
            roleSql, new { UserId = user.Id }, session.Transaction, cancellationToken: cancellationToken));
        if (insertedRoles != 1)
        {
            throw new InvalidOperationException("Le rôle USER est introuvable.");
        }

        return user;
    }

    public async Task UpdateGoogleAvatarAsync(long userId, string pictureUrl, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE tools_core.user_auth_provider
            SET provider_avatar_url = @PictureUrl
            WHERE user_id = @UserId AND provider = 'GOOGLE'
            """;
        await Connection().ExecuteAsync(new CommandDefinition(
            sql, new { UserId = userId, PictureUrl = pictureUrl }, session.Transaction, cancellationToken: cancellationToken));
    }

    private Npgsql.NpgsqlConnection Connection() => session.Connection
        ?? throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
}
