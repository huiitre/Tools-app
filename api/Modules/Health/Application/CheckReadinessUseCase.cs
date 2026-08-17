namespace Tools.ApiCore.Modules.Health.Application;

public class CheckReadinessUseCase
{
    private readonly IHealthRepository healthRepository;
    private readonly ILogger<CheckReadinessUseCase> logger;

    public CheckReadinessUseCase(
        IHealthRepository healthRepository,
        ILogger<CheckReadinessUseCase> logger)
    {
        this.healthRepository = healthRepository;
        this.logger = logger;
    }

    public async Task<bool> Execute()
    {
        logger.LogTrace("Début du use case de vérification de readiness.");

        return await healthRepository.IsReadyAsync();
    }
}
