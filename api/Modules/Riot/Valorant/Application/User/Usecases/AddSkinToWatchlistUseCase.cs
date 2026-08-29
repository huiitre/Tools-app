using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Skin.Views;
using Tools.Api.Modules.Riot.Valorant.Application.User.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.User.Ports;

namespace Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;

public sealed class AddSkinToWatchlistUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository,
    IValorantSkinRepository skinRepository,
    IValorantWatchlistRepository watchlistRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<ValorantSkinView> Execute(AddToWatchlistCommand command)
    {
        if (!await valorantAuthRepository.ExistsByIdAndUserId(command.AccountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        if (await watchlistRepository.ExistsByAccountIdAndSkinId(command.AccountId, command.SkinId))
        {
            throw AppException.Conflict("SKIN_ALREADY_IN_WATCHLIST", "Ce skin est déjà suivi.");
        }

        await watchlistRepository.Add(command.AccountId, command.SkinId);

        // Relu après l'ajout : la vue renvoyée porte le drapeau « suivi » à jour.
        var skin = await skinRepository.FindById(command.SkinId, command.AccountId);

        return skin ?? throw AppException.NotFound(
            "VALORANT_SKIN_NOT_FOUND",
            $"Le skin {command.SkinId} est introuvable.");
    }
}
