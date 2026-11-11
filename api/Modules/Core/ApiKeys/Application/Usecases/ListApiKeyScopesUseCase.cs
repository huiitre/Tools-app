using Tools.Api.Modules.Core.ApiKeys.Domain;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.ApiKeys.Application.Usecases;

// US-4 : le catalogue vient du code, jamais de la base.
public sealed class ListApiKeyScopesUseCase(UseCaseAuthorizer authorizer) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public IReadOnlyList<string> Execute() => ApiKeyScopes.AllCodes.Order().ToList();
}
