using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.User.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;

public sealed class GetMyValorantStoreHistoryUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository,
    IValorantStoreHistoryRepository storeHistoryRepository,
    IValorantSkinRepository skinRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<List<ValorantStoreHistoryView>> Execute(long accountId)
    {
        if (!await valorantAuthRepository.ExistsByIdAndUserId(accountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        var skinIdsByDate = await storeHistoryRepository.FindAllRawByAccountId(accountId);
        var history = new List<ValorantStoreHistoryView>();

        // TODO: une requête par skin, comme le Java. À regrouper si l'historique s'allonge.
        foreach (var (date, skinIds) in skinIdsByDate)
        {
            var skins = new List<ValorantSkinView>();

            foreach (var skinId in skinIds)
            {
                // Un skin disparu du catalogue est simplement omis de sa journée.
                if (await skinRepository.FindById(skinId, accountId) is { } skin)
                {
                    skins.Add(skin);
                }
            }

            history.Add(new ValorantStoreHistoryView(date, skins));
        }

        return history;
    }
}
