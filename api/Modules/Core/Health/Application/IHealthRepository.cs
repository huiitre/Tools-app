namespace Tools.Api.Modules.Core.Health.Application;

public interface IHealthRepository
{
    Task<bool> IsReadyAsync();
}
