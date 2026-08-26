using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Common.Application.Ports;
using Tools.Api.Modules.Core.Realtime.Application;
using Tools.Api.Modules.Core.Realtime.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Users.Application.Usecases;

// Cas d'usage administrateur : fixer le rôle global d'un utilisateur.
//
// Le rôle attribué remplace le précédent — il ne s'y ajoute pas. C'est ce que la clé primaire
// de user_role, (user_id), impose depuis V2.69.0 : l'écriture est un upsert.
public sealed class SetUserGlobalRoleUseCase(
    UseCaseAuthorizer authorizer,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ITransactionManager transactionManager,
    ILogger<SetUserGlobalRoleUseCase> logger,
    RealtimeEventService realtimeEventService
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(SetUserGlobalRoleCommand command)
    {
        // Les deux existences sont vérifiées avant d'ouvrir la transaction : rien à annuler
        // si l'une manque, et le message d'erreur distingue les deux causes.
        if (!await userRepository.ExistsAsync(command.UserId))
        {
            throw AppException.NotFound("USER_NOT_FOUND", "Utilisateur introuvable.");
        }

        if (!await roleRepository.ExistsAsync(command.RoleId))
        {
            throw AppException.NotFound("ROLE_NOT_FOUND", "Rôle introuvable.");
        }

        // Suppression puis insertion : sans transaction, un échec au milieu laisserait
        // l'utilisateur sans aucun rôle, donc incapable d'agir.
        await using var transaction = await transactionManager.BeginAsync();
        await userRepository.ReplaceGlobalRoleAsync(command.UserId, command.RoleId);
        await transaction.CommitAsync();

        // Journalisé après le commit : une trace ne doit jamais affirmer un changement que la
        // transaction aurait annulé. L'acteur est tracé autant que la cible — c'est ce qui
        // permet de répondre à « qui a donné ce rôle ».
        logger.LogInformation(
            "Rôle global modifié par userId={ActorId} : cible={TargetUserId} roleId={RoleId}",
            CurrentUser.UserId,
            command.UserId,
            command.RoleId);

        try
        {
            await realtimeEventService.PublishAsync(
                PublishRealtimeEventCommand.ForUser(command.UserId, "Core.UserGlobalRoleChanged"));
        } catch(Exception ex)
        {
            logger.LogWarning(ex, "Push temps réel du changement de rôle échoué pour userId={UserId}", command.UserId);
        }
    }
}
