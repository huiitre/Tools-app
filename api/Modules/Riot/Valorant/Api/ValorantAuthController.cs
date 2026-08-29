using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Commands;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Usecases;
using Tools.Api.Modules.Riot.Valorant.Application.Core.Views;

namespace Tools.Api.Modules.Riot.Valorant.Api;

// Comptes Valorant liés à un compte Tools.
//
// Les use cases sont résolus par action ([FromServices]) : un use case sécurisé applique son
// contrôle dès sa construction, une action ne doit donc construire que celui dont elle se sert.
// Aucun attribut d'autorisation ici, le rôle exigé est porté par le use case.
[ApiController]
[Route("riot/valorant/accounts")]
public class ValorantAuthController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> LinkAccount(
        [FromServices] LinkValorantAccountUseCase linkValorantAccountUseCase,
        [FromBody] LinkValorantAccountCommand command
    )
    {
        var account = await linkValorantAccountUseCase.Execute(command);

        return StatusCode(StatusCodes.Status201Created, account);
    }

    [HttpGet]
    public Task<List<ValorantAccountView>> ListAccounts(
        [FromServices] ListValorantAccountsUseCase listValorantAccountsUseCase
    )
    {
        return listValorantAccountsUseCase.Execute();
    }

    [HttpGet("{accountId:long}/refresh")]
    public Task<ValorantTokenView> Refresh(
        [FromServices] GetValorantAccessTokenUseCase getValorantAccessTokenUseCase,
        [FromRoute] long accountId
    )
    {
        return getValorantAccessTokenUseCase.Execute(accountId);
    }

    [HttpDelete("{accountId:long}")]
    public async Task<IActionResult> UnlinkAccount(
        [FromServices] UnlinkValorantAccountUseCase unlinkValorantAccountUseCase,
        [FromRoute] long accountId
    )
    {
        await unlinkValorantAccountUseCase.Execute(accountId);

        return NoContent();
    }

    [HttpPut("{accountId:long}")]
    public Task<ValorantAccountView> RenameAccount(
        [FromServices] RenameValorantAccountUseCase renameValorantAccountUseCase,
        [FromRoute] long accountId,
        [FromBody] RenameValorantAccountCommand command
    )
    {
        return renameValorantAccountUseCase.Execute(accountId, command);
    }
}
