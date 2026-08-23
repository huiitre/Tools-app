using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Domain;

public sealed class Expedition
{
    public string Name { get; private set; }
    public string Source { get; }
    public string RouteData { get; }

    private Expedition(string routeData, string name, string source)
    {
        Name = name;
        Source = source;
        RouteData = routeData;
    }

    public static Expedition Create(string routeData, string name, string source)
    {
        if (string.IsNullOrWhiteSpace(routeData)) throw AppException.Validation("R2R_ROUTE_DATA_REQUIRED", "Le fichier de route est obligatoire.");

        if (string.IsNullOrWhiteSpace(name)) throw AppException.Validation("R2R_NAME_REQUIRED", "Le nom de l'expédition est obligatoire.");

        if (string.IsNullOrWhiteSpace(source)) throw AppException.Validation("R2R_SOURCE_REQUIRED", "La source de l'import est obligatoire.");

        return new Expedition(routeData, name, source);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw AppException.Validation("R2R_NAME_REQUIRED", "Le nom de l'expédition est obligatoire.");

        Name = name;
    }
}