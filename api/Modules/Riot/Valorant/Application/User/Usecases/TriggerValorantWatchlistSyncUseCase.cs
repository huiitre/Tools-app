using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.Riot.Valorant.Application.User.Services;

namespace Tools.Api.Modules.Riot.Valorant.Application.User.Usecases;

// Déclenchement manuel de la passe que le planificateur fait seul.
//
// Le Java portait ce contrôle sur la route (@RequiredRole(ADMIN)) et appelait le notifieur
// directement. Ici la route n'en porte aucun : sans ce use case, l'action serait ouverte à tous.
public sealed class TriggerValorantWatchlistSyncUseCase(
    UseCaseAuthorizer authorizer,
    ValorantWatchlistNotifier valorantWatchlistNotifier
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;
    protected override ModuleCode? RequiredModule => ModuleCode.Riot;

    public Task Execute()
    {
        return valorantWatchlistNotifier.ProcessAllAccounts();
    }
}
