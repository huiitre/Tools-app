namespace Tools.Api.Modules.Health.Application;

public interface IHealthRepository
{
    Task<bool> IsReadyAsync();
}
