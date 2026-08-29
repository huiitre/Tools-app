using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Riot.Common.Infrastructure;

// Lecture défensive des fichiers de l'extracteur : un champ absent vaut « pas de valeur », jamais
// une exception — sauf l'identifiant, sans lequel la ligne ne veut rien dire.
public static class ValorantAssetJson
{
    public static Guid RequiredUuid(JsonElement element, string propertyName) =>
        OptionalUuid(element, propertyName)
        ?? throw AppException.Unavailable(
            "VALORANT_ASSET_INVALID",
            $"Une entrée des données Valorant n'a pas de « {propertyName} » exploitable.");

    public static Guid? OptionalUuid(JsonElement element, string propertyName) =>
        OptionalString(element, propertyName) is { } value && Guid.TryParse(value, out var uuid)
            ? uuid
            : null;

    public static string? OptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
        && property.GetString() is { Length: > 0 } value
            ? value
            : null;

    public static int Int32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : 0;

    public static List<JsonElement> Array(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array
            ? [.. array.EnumerateArray()]
            : [];
}
