using Tools.Api.Modules.Core.Access.Application.Ports;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Realtime.Application;
using Tools.Api.Modules.Core.Realtime.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Access.Application.Usecases;

// Cas d'usage administrateur : modifier un module fonctionnel, activation comprise.
public sealed class UpdateModuleUseCase(
    UseCaseAuthorizer authorizer,
    IModuleRepository moduleRepository,
    ILogger<UpdateModuleUseCase> logger,
    RealtimeEventService realtimeEventService
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(UpdateModuleCommand command)
    {
        if (!await moduleRepository.ExistsAsync(command.ModuleId))
        {
            throw AppException.NotFound("MODULE_NOT_FOUND", "Module introuvable.");
        }

        var code = command.Code.Trim();

        // Le module qu'on modifie est exclu du contrôle : garder son propre code n'est pas
        // un conflit.
        if (await moduleRepository.CodeExistsAsync(code, command.ModuleId))
        {
            throw AppException.Conflict("MODULE_CODE_ALREADY_EXISTS", "Ce code de module est déjà utilisé.");
        }

        await moduleRepository.UpdateAsync(
            command.ModuleId,
            code,
            command.Name.Trim(),
            command.Description?.Trim(),
            command.Active);

        // L'activation est la modification qui a un effet visible pour les utilisateurs :
        // elle mérite d'apparaître explicitement dans la trace.
        logger.LogInformation(
            "Module modifié par userId={ActorId} : moduleId={ModuleId} code={Code} actif={Active}",
            CurrentUser.UserId,
            command.ModuleId,
            code,
            command.Active
        );

        try
        {
            // Toucher le module lui-même concerne tous ses membres, pas un seul utilisateur —
            // TargetModuleId résout la liste, on ne la connaît pas ici.
            await realtimeEventService.PublishAsync(
                PublishRealtimeEventCommand.ForModule(command.ModuleId, "Core.ModuleUpdated"));
        } catch(Exception ex)
        {
            logger.LogWarning(ex, "Push temps réel de la mise à jour du module échoué pour moduleId={ModuleId}", command.ModuleId);
        }
    }
}
