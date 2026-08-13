using Npgsql;

public sealed class PostgresTransactionManager(
    NpgsqlDataSource dataSource,
    PostgresSession session) : ITransactionManager
{
    public async Task<ITransaction> BeginAsync()
    {
        if (session.Transaction is not null)
        {
            throw new InvalidOperationException("Une transaction est déjà ouverte pour cette requête.");
        }

        var connection = await dataSource.OpenConnectionAsync();
        var transaction = await connection.BeginTransactionAsync();
        session.Start(connection, transaction);
        return new PostgresTransaction(session, connection, transaction);
    }

    private sealed class PostgresTransaction(
        PostgresSession session,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction) : ITransaction
    {
        private bool completed;

        public async Task CommitAsync()
        {
            await transaction.CommitAsync();
            completed = true;
        }

        public async Task RollbackAsync()
        {
            if (!completed)
            {
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
                    await transaction.RollbackAsync();
                }
            }
            finally
            {
                session.Clear();
                await transaction.DisposeAsync();
                await connection.DisposeAsync();
            }
        }
    }
}
