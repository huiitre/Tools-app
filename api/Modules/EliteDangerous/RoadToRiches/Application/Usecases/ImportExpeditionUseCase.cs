using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.Core.Security.Application.Services;
using Tools.Api.Modules.Core.Security.Application.Usecases;
using Tools.Api.Modules.Core.Security.Domain;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Commands;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Domain;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Usecases;

public sealed class ImportExpeditionUseCase(
    UseCaseAuthorizer authorizer,
    IEnumerable<IRouteImporter> routeImporters,
    IExpeditionRepository expeditionRepository
) : SecuredUseCase(authorizer)
{
    protected override RoleCode RequiredRole => RoleCode.User;
    protected override ModuleCode? RequiredModule => ModuleCode.EliteDangerous;

    public async Task<Guid> Execute(ImportExpeditionCommand command)
    {
        // Le format du fichier appartient à son importeur : le use case ne fait que le choisir.
        var importer = routeImporters.FirstOrDefault(importer => importer.Source == command.Source)
            ?? throw AppException.Validation(
                "R2R_UNKNOWN_SOURCE",
                $"La source « {command.Source} » n'est pas reconnue.");

        var routeData = importer.Parse(command.FileContent, command.FileName);
        var name = await ResolveName(command.Name, command.FileName);

        var expedition = Expedition.Create(routeData, name, command.Source);

        return await expeditionRepository.Save(CurrentUser.UserId, expedition);
    }

    // Le nom est facultatif : à défaut, le fichier le donne, et sinon on numérote.
    private async Task<string> ResolveName(string? providedName, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(providedName))
        {
            return providedName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);

            if (!string.IsNullOrWhiteSpace(baseName))
            {
                return baseName;
            }
        }

        var count = await expeditionRepository.CountByUserId(CurrentUser.UserId);

        return $"Expédition #{count + 1}";
    }
}
