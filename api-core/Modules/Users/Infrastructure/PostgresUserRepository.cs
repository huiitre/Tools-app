using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Common.Infrastructure;
using Tools.ApiCore.Modules.Users.Application;
using Tools.ApiCore.Modules.Users.Domain;

namespace Tools.ApiCore.Modules.Users.Infrastructure;

public class PostgresUserRepository : IUserRepository
{
    private readonly PostgresSession session;
    private readonly ILogger<PostgresUserRepository> logger;

    public PostgresUserRepository(
        PostgresSession session,
        ILogger<PostgresUserRepository> logger)
    {
        this.session = session;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        logger.LogDebug("Lecture de tous les utilisateurs avec Dapper.");
        const string sql = """
            SELECT id AS Id, name AS Name
            FROM users
            ORDER BY id;
            """;

        var connection = GetCurrentConnection();
        var rows = await connection.QueryAsync<UserRow>(sql, transaction: session.Transaction);

        var users = rows
            .Select(row => User.Rehydrate(row.Id, row.Name))
            .ToList();

        logger.LogInformation("{UserCount} utilisateur(s) lu(s).", users.Count);

        return users;
    }

    public async Task<IReadOnlyList<User>> GetAllNative()
    {
        const string sql = """
            SELECT id, name
            FROM users
            ORDER BY id;
            """;

        await using var command = GetCurrentConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = session.Transaction;
        await using var reader = await command.ExecuteReaderAsync();

        var users = new List<User>();

        while (await reader.ReadAsync())
        {
            var user = User.Rehydrate(
                reader.GetInt64(0),
                reader.GetString(1));

            users.Add(user);
        }

        return users;
    }

    public async Task<User> CreateAsync(User user)
    {
        logger.LogDebug("Insertion de l'utilisateur {UserName} avec Dapper.", user.Name);
        const string sql = """
            INSERT INTO users (name)
            VALUES (@Name)
            RETURNING id;
            """;

        var connection = GetCurrentConnection();
        var id = await connection.QuerySingleAsync<long>(sql, new { user.Name }, session.Transaction);

        logger.LogInformation("Utilisateur {UserName} créé avec l'identifiant {UserId}.", user.Name, id);

        return User.Rehydrate(id, user.Name);
    }

    public async Task<User> CreateNative(User user)
    {
        const string sql = """
            INSERT INTO users (name)
            VALUES ($1)
            RETURNING id;
            """;

        await using var command = GetCurrentConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = session.Transaction;
        command.Parameters.AddWithValue(user.Name);

        var id = (long)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("L'insertion de l'utilisateur n'a retourné aucun identifiant"));

        return User.Rehydrate(id, user.Name);
    }

    private sealed record UserRow(long Id, string Name);

    private NpgsqlConnection GetCurrentConnection()
    {
        if (session.Connection is null)
        {
            logger.LogError("Le repository a été utilisé sans transaction PostgreSQL ouverte.");
            throw new InvalidOperationException("Aucune transaction PostgreSQL n'est ouverte.");
        }

        return session.Connection;
    }
}
