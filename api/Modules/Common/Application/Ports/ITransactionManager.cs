namespace Tools.ApiCore.Modules.Common.Application.Ports;

public interface ITransactionManager
{
    Task<ITransaction> BeginAsync();
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}
