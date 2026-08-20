using Tools.Api.Modules.Core.Access.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Access.Application.Usecases;

// Cas d'usage administrateur : créer un module fonctionnel.
//
// Le module est créé inactif. C'est le comportement de l'API Java, et il est volontaire :
// activer est un second geste, une fois le module configuré et ses membres désignés.
public sealed class CreateModuleUseCase(
    UseCaseAuthorizer authorizer,
    IModuleRepository moduleRepository,
    ILogger<CreateModuleUseCase> logger
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task<long> Execute(CreateModuleCommand command)
    {
        var code = command.Code.Trim();
        var name = command.Name.Trim();

        // Le code est unique en base. Le vérifier ici rend l'erreur exploitable par le
        // frontend au lieu d'une violation de contrainte remontée en 500.
        if (await moduleRepository.CodeExistsAsync(code))
        {
            throw AppException.Conflict("MODULE_CODE_ALREADY_EXISTS", "Ce code de module est déjà utilisé.");
        }

        var moduleId = await moduleRepository.CreateAsync(code, name, command.Description?.Trim());

        logger.LogInformation(
            "Module créé par userId={ActorId} : moduleId={ModuleId} code={Code}",
            CurrentUser.UserId,
            moduleId,
            code);

        return moduleId;
    }
}
