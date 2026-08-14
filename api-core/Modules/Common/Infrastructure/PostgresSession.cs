using Npgsql;

namespace Tools.ApiCore.Modules.Common.Infrastructure;

public sealed class PostgresSession
{
    public NpgsqlConnection? Connection { get; private set; }
    public NpgsqlTransaction? Transaction { get; private set; }

    public void Start(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public void Clear()
    {
        Connection = null;
        Transaction = null;
    }
}
