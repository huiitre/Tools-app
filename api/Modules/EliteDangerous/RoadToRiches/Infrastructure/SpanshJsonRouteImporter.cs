using System.Text;
using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;
using Tools.Api.Modules.EliteDangerous.RoadToRiches.Application.Ports;

namespace Tools.Api.Modules.EliteDangerous.RoadToRiches.Infrastructure;

public sealed class SpanshJsonRouteImporter : IRouteImporter
{
    public string Source => "spansh";

    // Le document entier est conservé : le frontend lit `parameters` autant que `result`.
    public string Parse(byte[] fileContent, string fileName)
    {
        using var document = JsonDocument.Parse(fileContent);

        if (!document.RootElement.TryGetProperty("result", out var result)
            || result.ValueKind != JsonValueKind.Array)
        {
            throw AppException.Validation(
                "R2R_INVALID_JSON_FORMAT",
                $"Le fichier {fileName} n'est pas un export Spansh valide.");
        }

        return Encoding.UTF8.GetString(fileContent);
    }
}
