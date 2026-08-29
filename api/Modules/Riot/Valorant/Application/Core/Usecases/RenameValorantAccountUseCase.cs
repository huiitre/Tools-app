using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;

public sealed class RenameValorantAccountUseCase(
    UseCaseAuthorizer authorizer,
    IValorantAuthRepository valorantAuthRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<ValorantAccountView> Execute(long accountId, RenameValorantAccountCommand command)
    {
        if (!await valorantAuthRepository.ExistsByIdAndUserId(accountId, CurrentUser.UserId))
        {
            throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");
        }

        if (string.IsNullOrWhiteSpace(command.Label))
        {
            throw AppException.Validation("LABEL_REQUIRED", "Le libellé est obligatoire.");
        }

        await valorantAuthRepository.UpdateLabel(accountId, command.Label.Trim());

        var accounts = await valorantAuthRepository.FindAllByUserId(CurrentUser.UserId);
        var renamed = accounts.FirstOrDefault(account => account.Id == accountId)
            ?? throw AppException.NotFound(
                "VALORANT_ACCOUNT_NOT_FOUND",
                "Ce compte Valorant est introuvable.");

        return new ValorantAccountView(
            renamed.Id,
            renamed.Puuid,
            renamed.Region,
            renamed.GameName,
            renamed.TagLine,
            renamed.Label);
    }
}
