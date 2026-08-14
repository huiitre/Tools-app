namespace Tools.ApiCore.Modules.Health.Application;

public interface IHealthRepository
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken);
}
