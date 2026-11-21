namespace Tools.Api.Modules.Core.AndroidApps.Application.Commands;

// Le libellé ne sert qu'à une nouvelle app : celui d'une app existante se change par UpdateAndroidApp.
public sealed record ConfirmAndroidAppImportCommand(
    Guid ImportId,
    string? Label,
    bool Recommend,
    string? ReleaseNotes);

// Champ absent = inchangé. Une app ne peut pas revenir à « aucune version recommandée » : pour la
// retirer du catalogue, on la désactive. AdminNotes vide efface les notes.
public sealed record UpdateAndroidAppCommand(
    string? Label,
    int? DisplayOrder,
    bool? IsActive,
    long? RecommendedReleaseId,
    string? AdminNotes);
