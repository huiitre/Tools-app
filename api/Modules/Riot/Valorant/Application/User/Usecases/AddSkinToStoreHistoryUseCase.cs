using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.User.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;

// Archive la rotation du jour d'un coup : le front envoie toute la boutique en un appel.
public sealed class AddSkinToStoreHistoryUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository,
    IValorantStoreHistoryRepository storeHistoryRepository,
    ITransactionManager transactionManager
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task Execute(AddSkinToStoreHistoryCommand command)
    {
        if (!await valorantAuthRepository.ExistsByIdAndUserId(command.AccountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        if (command.SkinIds is not { Count: > 0 } skinIds)
        {
            return;
        }

        // Le Java retombait sur la date du jour quand l'appelant n'en donnait pas.
        var seenAt = command.SeenAt ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Le lot est indivisible : une rotation à moitié archivée serait relue comme incomplète.
        await using var transaction = await transactionManager.BeginAsync();

        foreach (var skinId in skinIds)
        {
            if (!await storeHistoryRepository.ExistsByAccountIdAndSkinIdAndDate(
                    command.AccountId, skinId, seenAt))
            {
                await storeHistoryRepository.Add(command.AccountId, skinId, seenAt);
            }
        }

        await transaction.CommitAsync();
    }
}
