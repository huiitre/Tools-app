using Tools.ApiCore.Modules.Common.Application.Exceptions;
using Tools.ApiCore.Modules.Common.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Ports;
using Tools.ApiCore.Modules.Security.Application.Services;
using Tools.ApiCore.Modules.Security.Application.Usecases;
using Tools.ApiCore.Modules.Security.Domain;

namespace Tools.ApiCore.Modules.Users.Application.Usecases;

// Cas d'usage administrateur : fixer le rôle global d'un utilisateur.
//
// Le rôle attribué remplace le précédent — il ne s'y ajoute pas. La table user_role autorise
// pourtant le cumul, et c'est bien un remplacement que l'API Java opère déjà : le frontend
// n'offre qu'un rôle à la fois.
public sealed class SetUserGlobalRoleUseCase(
    UseCaseAuthorizer authorizer,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ITransactionManager transactionManager
) : SecuredUseCase<SetUserGlobalRoleCommand>(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    protected override async Task Handle(SetUserGlobalRoleCommand command, CurrentUser currentUser)
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
    }
}
