using System.Text.Json;
using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Temtem.Sync.Infrastructure;

// Lecture défensive : un champ absent vaut « pas de valeur », sauf ceux qui identifient la ligne.
public static class TemtemAssetJson
{
    public static int RequiredInt(JsonElement element, string propertyName) =>
        OptionalInt(element, propertyName)
        ?? throw AppException.Unavailable(
            "TEMTEM_ASSET_INVALID",
            $"Une entrée des données Temtem n'a pas de « {propertyName} » exploitable.");

    public static string RequiredString(JsonElement element, string propertyName) =>
        OptionalString(element, propertyName)
        ?? throw AppException.Unavailable(
            "TEMTEM_ASSET_INVALID",
            $"Une entrée des données Temtem n'a pas de « {propertyName} » exploitable.");

    public static int? OptionalInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : null;

    public static string? OptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
        && property.ValueKind == JsonValueKind.String
        && property.GetString() is { Length: > 0 } value
            ? value
            : null;

    public static decimal RequiredDecimal(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetDecimal()
            : throw AppException.Unavailable(
                "TEMTEM_ASSET_INVALID",
                $"Une entrée des données Temtem n'a pas de « {propertyName} » numérique.");

    public static JsonElement RequiredObject(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : throw AppException.Unavailable(
                "TEMTEM_ASSET_INVALID",
                $"Une entrée des données Temtem n'a pas d'objet « {propertyName} ».");
}
