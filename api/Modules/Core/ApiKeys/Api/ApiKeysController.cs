using Microsoft.AspNetCore.Mvc;
using Tools.Api.Modules.Core.ApiKeys.Application.Commands;
using Tools.Api.Modules.Core.ApiKeys.Application.Usecases;
using Tools.Api.Modules.Core.ApiKeys.Application.Views;

namespace Tools.Api.Modules.Core.ApiKeys.Api;

[ApiController]
[Route("admin/api-keys")]
public sealed class ApiKeysController : ControllerBase
{
    [HttpGet]
    public Task<List<ApiKeyView>> List(
        [FromServices] ListApiKeysUseCase listApiKeysUseCase)
    {
        return listApiKeysUseCase.Execute();
    }

    [HttpGet("scopes")]
    public IReadOnlyList<string> ListScopes(
        [FromServices] ListApiKeyScopesUseCase listApiKeyScopesUseCase)
    {
        return listApiKeyScopesUseCase.Execute();
    }

    // Seule réponse qui contienne la clé en clair.
    [HttpPost]
    public Task<CreatedApiKeyView> Create(
        [FromServices] CreateApiKeyUseCase createApiKeyUseCase,
        CreateApiKeyRequest request)
    {
        return createApiKeyUseCase.Execute(new CreateApiKeyCommand(
            request.Name, request.Description, request.ExpiresAt, request.Scopes));
    }

    // POST et non DELETE : la clé reste en base, seule sa validité change.
    [HttpPost("{apiKeyId:long}/revoke")]
    public async Task<IActionResult> Revoke(
        [FromServices] RevokeApiKeyUseCase revokeApiKeyUseCase,
        [FromRoute] long apiKeyId)
    {
        await revokeApiKeyUseCase.Execute(apiKeyId);

        return NoContent();
    }

    public sealed record CreateApiKeyRequest(
        string Name,
        string Description,
        DateTimeOffset ExpiresAt,
        IReadOnlyCollection<string> Scopes);
}
