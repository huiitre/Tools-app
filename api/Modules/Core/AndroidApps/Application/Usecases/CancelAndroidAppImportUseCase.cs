using Tools.Api.Modules.Core.AndroidApps.Application.Ports;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;

namespace Tools.Api.Modules.Core.AndroidApps.Application.Usecases;

// US-1, popup fermée sans valider : le fichier en attente est supprimé tout de suite plutôt qu'à
// son expiration. Sans erreur s'il n'existe déjà plus.
public sealed class CancelAndroidAppImportUseCase(
    UseCaseAuthorizer authorizer,
    IAndroidAppFileStore fileStore
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.Admin;

    public void Execute(Guid importId) => fileStore.DeleteStaged(importId);
}
