using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Realtime.Application;
using Tools.Api.Modules.Core.Realtime.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.Users.Application.Usecases;

// Cas d'usage administrateur : autoriser ou refuser la connexion d'un compte.
//
// `is_active` répond à une seule question — ce compte peut-il ouvrir une session — et ne dit
// rien de la confirmation de l'adresse email, que `email_verified_at` porte séparément depuis
// V2.65.0. Suspendre un compte ne défait donc pas sa vérification.
//
// La suspension ne casse pas la session déjà ouverte : l'access token porte `isActive` figé à
// son émission, et reste donc accepté jusqu'à son expiration. Le renouvellement, lui, relit
// `is_active` et refuse (`RefreshSessionUseCase`) — la suspension prend effet au plus tard au
// premier refresh. L'event temps réel poussé ci-dessous ne fait qu'écourter cette attente pour
// un client connecté ; il ne la garantit pas, et ne remplace donc aucun contrôle serveur.
public sealed class SetUserActiveUseCase(
    UseCaseAuthorizer authorizer,
    IUserRepository userRepository,
    RealtimeEventService realtimeEventService,
    ILogger<SetUserActiveUseCase> logger
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public async Task Execute(SetUserActiveCommand command)
    {
        // Se suspendre soi-même retire le droit d'annuler l'opération : il faudrait un autre
        // administrateur, ou un UPDATE à la main en base. La réactivation de son propre compte
        // reste permise, elle ne ferme aucune porte.
        if (!command.Active && command.UserId == CurrentUser.UserId)
        {
            throw AppException.Conflict(
                "CANNOT_DEACTIVATE_SELF",
                "Un administrateur ne peut pas désactiver son propre compte.");
        }

        if (!await userRepository.ExistsAsync(command.UserId))
        {
            throw AppException.NotFound("USER_NOT_FOUND", "Utilisateur introuvable.");
        }

        await userRepository.SetActiveAsync(command.UserId, command.Active);

        // L'acteur est tracé autant que la cible : c'est ce qui permet de répondre à « qui a
        // suspendu ce compte ».
        logger.LogInformation(
            "Statut de compte modifié par userId={ActorId} : cible={TargetUserId} active={Active}",
            CurrentUser.UserId,
            command.UserId,
            command.Active);

        // Poussé après l'écriture : un event ne doit jamais annoncer un changement qui n'a pas
        // eu lieu. L'event nomme le fait et transporte le nouvel état, plutôt que d'exister en
        // deux versions — au front de décider ce qu'il en fait.
        try
        {
            await realtimeEventService.PublishAsync(
                PublishRealtimeEventCommand.ForUser(
                    command.UserId,
                    "Core.UserActiveChanged",
                    new { active = command.Active }));
        }
        catch (Exception exception)
        {
            // Le compte est déjà suspendu quand on arrive ici : l'échec du push est une
            // information dégradée, pas une raison de refuser l'opération à l'administrateur.
            logger.LogWarning(
                exception,
                "Push temps réel du changement de statut échoué pour userId={UserId}",
                command.UserId);
        }
    }
}
