using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.Catalog.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Ports;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Services;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Views;

namespace Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;

// Lie un compte Valorant à l'appelant à partir d'un refresh token relevé par l'utilisateur dans
// les cookies du client Riot. L'échange ne peut pas se faire depuis le navigateur (CORS).
public sealed class LinkValorantAccountUseCase(
    UseCaseAuthorizer authorizer,
    IRiotAuthPort riotAuthPort,
    IValorantStorePort valorantStorePort,
    IValorantVersionProvider versionProvider,
    ValorantAuthService valorantAuthService,
    ILogger<LinkValorantAccountUseCase> logger
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public async Task<ValorantAccountAuthView> Execute(LinkValorantAccountCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            throw AppException.Validation("REFRESH_TOKEN_REQUIRED", "Le refresh token est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(command.Region))
        {
            throw AppException.Validation("REGION_REQUIRED", "La région est obligatoire.");
        }

        // Le premier échange vaut validation : un refresh token que Riot refuse ne sera pas rangé.
        var riotResponse = await riotAuthPort.Refresh(command.RefreshToken);

        var riotId = await ResolveRiotId(riotResponse, command.Region);

        var accountId = await valorantAuthService.SaveAuthData(
            CurrentUser.UserId,
            riotResponse.Puuid,
            command.Region,
            riotId.GameName,
            riotId.TagLine,
            command.Label,
            riotResponse.RefreshToken,
            riotResponse.RefreshTokenExpiresAt);

        var account = new ValorantAccountView(
            accountId,
            riotResponse.Puuid,
            command.Region,
            riotId.GameName,
            riotId.TagLine,
            command.Label);

        // L'access token repart avec la réponse : le front s'en sert tout de suite, il n'est
        // jamais stocké.
        return new ValorantAccountAuthView(account, riotResponse.AccessToken);
    }

    // Le pseudo est un confort d'affichage : son absence ne doit pas empêcher de lier le compte.
    private async Task<IValorantStorePort.RiotId> ResolveRiotId(
        IRiotAuthPort.ValorantAuthResponse riotResponse,
        string region)
    {
        try
        {
            var entitlementsToken = await valorantStorePort.FetchEntitlementsToken(riotResponse.AccessToken);
            var clientVersion = await versionProvider.GetRiotClientVersion();

            return await valorantStorePort.FetchRiotId(
                riotResponse.Puuid, region, riotResponse.AccessToken, entitlementsToken, clientVersion);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "Riot ID non résolu pour le puuid {Puuid} : {Message}",
                riotResponse.Puuid,
                exception.Message);

            return new IValorantStorePort.RiotId(null, null);
        }
    }
}
