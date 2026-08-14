using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Users.Domain;

namespace Tools.ApiCore.Modules.Users.Application;

public class ListUsersUseCase
{

    private readonly IUserRepository userRepository;
    private readonly ITransactionManager transactionManager;
    private readonly ILogger<ListUsersUseCase> logger;

    public ListUsersUseCase(
        IUserRepository userRepository,
        ITransactionManager transactionManager,
        ILogger<ListUsersUseCase> logger)
    {
        this.userRepository = userRepository;
        this.transactionManager = transactionManager;
        this.logger = logger;
    }

    public async Task Execute()
    {
        logger.LogTrace("Début du use case de création d'utilisateurs.");

        await using var transaction = await transactionManager.BeginAsync();

        var firstUser = await userRepository.CreateAsync(User.Create("Yanis"));
        logger.LogDebug("Premier utilisateur créé : {UserId}.", firstUser.Id);

        var secondUser = await userRepository.CreateAsync(User.Create("Yanis"));
        logger.LogDebug("Deuxième utilisateur créé : {UserId}.", secondUser.Id);

        var thirdUser = await userRepository.CreateAsync(User.Create("Yanis"));
        logger.LogDebug("Troisième utilisateur créé : {UserId}.", thirdUser.Id);

        await transaction.CommitAsync();
        logger.LogInformation("Les trois utilisateurs ont été créés et la transaction a été validée.");
    }
}
