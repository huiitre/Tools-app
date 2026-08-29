using Dapper;
using Npgsql;
using Tools.Api.Modules.Core.Common.Infrastructure;

namespace Tools.Api.Modules.Riot.Common.Infrastructure;

// Accès Dapper partagé par les adaptateurs PostgreSQL du module.
//
// Il rejoint la transaction ouverte par le use case quand il y en a une, et ouvre sa propre
// connexion sinon. Les deux cas existent réellement : l'historique de boutique s'écrit par lot
// sous transaction depuis le front, et une ligne à la fois depuis la passe de fond.
public sealed class RiotDatabase(NpgsqlDataSource dataSource, PostgresSession session)
{
    public Task<List<T>> Query<T>(string sql, object? parameters = null) =>
        Run(async (connection, transaction) =>
            (await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, transaction))).AsList());

    public Task<T?> QueryFirstOrDefault<T>(string sql, object? parameters = null) =>
        Run(async (connection, transaction) =>
            await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, transaction)));

    public Task<T> ExecuteScalar<T>(string sql, object? parameters = null) =>
        Run(async (connection, transaction) =>
            await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, transaction)))!;

    public Task<int> Execute(string sql, object? parameters = null) =>
        Run(async (connection, transaction) =>
            await connection.ExecuteAsync(new CommandDefinition(sql, parameters, transaction)));

    private async Task<T> Run<T>(Func<NpgsqlConnection, NpgsqlTransaction?, Task<T>> run)
    {
        if (session.Connection is { } ambientConnection)
        {
            return await run(ambientConnection, session.Transaction);
        }

        await using var connection = await dataSource.OpenConnectionAsync();

        return await run(connection, null);
    }
}
