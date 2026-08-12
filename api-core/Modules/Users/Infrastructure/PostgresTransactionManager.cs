using Npgsql;

public class PostgresTransactionManager : ITransactionManager
{
    private readonly NpgsqlDataSource dataSource;
    private readonly PostgresSession session;
    private readonly ILogger<PostgresTransactionManager> logger;

    public PostgresTransactionManager(
        NpgsqlDataSource dataSource,
        PostgresSession session,
        ILogger<PostgresTransactionManager> logger)
    {
        this.dataSource = dataSource;
        this.session = session;
        this.logger = logger;
    }

    public async Task<ITransaction> BeginAsync()
    {
        logger.LogTrace("Demande d'ouverture d'une transaction PostgreSQL.");

        if (session.Transaction is not null)
        {
            logger.LogError("Une seconde transaction PostgreSQL a été demandée dans la même requête.");
            throw new InvalidOperationException("Une transaction est déjà ouverte pour cette requête.");
        }

        var connection = await dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();

        session.Start(connection, transaction);
        logger.LogDebug("Transaction PostgreSQL ouverte.");

        return new PostgresTransaction(session, connection, transaction, logger);
    }

    private sealed class PostgresTransaction : ITransaction
    {
        private readonly PostgresSession session;
        private readonly NpgsqlConnection connection;
        private readonly NpgsqlTransaction transaction;
        private readonly ILogger logger;
        private bool completed;

        public PostgresTransaction(
            PostgresSession session,
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            ILogger logger)
        {
            this.session = session;
            this.connection = connection;
            this.transaction = transaction;
            this.logger = logger;
        }

        public async Task CommitAsync()
        {
            logger.LogInformation("Validation de la transaction PostgreSQL.");
            await transaction.CommitAsync();
            completed = true;
        }

        public async Task RollbackAsync()
        {
            if (!completed)
            {
                logger.LogWarning("Annulation explicite de la transaction PostgreSQL.");
                await transaction.RollbackAsync();
                completed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!completed)
                {
                    logger.LogWarning("Transaction non validée : rollback automatique pendant DisposeAsync.");
                    await transaction.RollbackAsync();
                }
            }
            finally
            {
                session.Clear();
                logger.LogTrace("Libération de la connexion et de la transaction PostgreSQL.");
                await transaction.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
