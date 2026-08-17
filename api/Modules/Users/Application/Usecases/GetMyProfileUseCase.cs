using Tools.Api.Modules.Security.Application.Ports;
using Tools.Api.Modules.Security.Application.Services;
using Tools.Api.Modules.Security.Application.Usecases;
using Tools.Api.Modules.Security.Domain;
using Tools.Api.Modules.Users.Application.Dto;

namespace Tools.Api.Modules.Users.Application.Usecases;

// Cas d'usage utilisateur : consulter son propre profil.
//
// Rôle minimum READ_ONLY : tout utilisateur authentifié peut lire son profil.
public sealed class GetMyProfileUseCase(
    UseCaseAuthorizer authorizer,
    IUserRepository userRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.ReadOnly;

    public async Task<UserProfileDto> Execute()
    {
        var userId = CurrentUser.UserId;

        UserProfileDto userProfile = await userRepository.FindProfileAsync(userId)
            ?? throw new InvalidOperationException($"Le profil de l'utilisateur {userId} n'a pas été trouvé.");

        return userProfile;
    }
}
