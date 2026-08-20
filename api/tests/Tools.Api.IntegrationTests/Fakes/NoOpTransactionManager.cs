using Tools.Api.Modules.Core.Common.Application.Ports;

namespace Tools.Api.IntegrationTests.Fakes;

// Transaction sans effet : les doubles mémoire n'ont rien à valider ni à annuler.
public sealed class NoOpTransactionManager : ITransactionManager
{
    public Task<ITransaction> BeginAsync() => Task.FromResult<ITransaction>(new NoOpTransaction());

    private sealed class NoOpTransaction : ITransaction
    {
        public Task CommitAsync() => Task.CompletedTask;
        public Task RollbackAsync() => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
