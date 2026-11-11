using Tools.Api.Modules.Core.ApiKeys.Application.Ports;
using Tools.Api.Modules.Core.ApiKeys.Application.Views;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Usecases;

// US-3 : toutes les clés, révoquées comprises ; l'interface les range à part grâce à IsRevoked.
public sealed class ListApiKeysUseCase(
    UseCaseAuthorizer authorizer,
    IApiKeyRepository apiKeyRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<List<ApiKeyView>> Execute()
    {
        var apiKeys = await apiKeyRepository.FindAll();
        return apiKeys.Select(ApiKeyView.From).ToList();
    }
}
