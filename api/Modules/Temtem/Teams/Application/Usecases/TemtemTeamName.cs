using Tools.Api.Modules.Core.Common.Application.Exceptions;

namespace Tools.Api.Modules.Temtem.Teams.Application.Usecases;

// Le nom d'équipe est saisi par l'utilisateur : deux use cases l'acceptent, une seule règle le
// décrit.
internal static class TemtemTeamName
{
    private const int MaxLength = 100;

    // Rend le nom nettoyé : c'est lui qui part en base, pas celui qui est arrivé.
    public static string Normalize(string? name)
    {
        var trimmed = name?.Trim();

        return string.IsNullOrEmpty(trimmed)
            ? throw AppException.Validation("TEAM_NAME_REQUIRED", "Le nom de l'équipe est obligatoire.")
            : trimmed.Length > MaxLength
                ? throw AppException.Validation(
                    "TEAM_NAME_TOO_LONG",
                    $"Le nom de l'équipe ne peut pas dépasser {MaxLength} caractères.")
                : trimmed;
    }
}
