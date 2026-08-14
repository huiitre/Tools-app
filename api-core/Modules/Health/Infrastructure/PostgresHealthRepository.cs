using Dapper;
using Npgsql;
using Tools.ApiCore.Modules.Health.Application;

namespace Tools.ApiCore.Modules.Health.Infrastructure;

public class PostgresHealthRepository : IHealthRepository
{
    private readonly NpgsqlDataSource dataSource;
    private readonly ILogger<PostgresHealthRepository> logger;

    public PostgresHealthRepository(
        NpgsqlDataSource dataSource,
        ILogger<PostgresHealthRepository> logger)
    {
        this.dataSource = dataSource;
        this.logger = logger;
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));

            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PostgreSQL est inaccessible.");

            return false;
        }
    }
}
