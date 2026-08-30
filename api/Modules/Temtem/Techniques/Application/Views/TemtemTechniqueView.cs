using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Techniques.Application.Views;

public sealed record TemtemCategoryView(string Code, string Label, string? ImageUrl);

public sealed record TemtemPriorityView(int Order, string Label, string? ImageUrl);

// Une technique, telle que le jeu la décrit. Réutilisée par la fiche d'un Temtem, la composition
// d'équipe et le simulateur — il n'y a pas de variante par écran.
public sealed record TemtemTechniqueView(
    int Id,
    string Slug,
    string Name,
    string? Effect,
    TemtemTypeView Type,
    TemtemCategoryView Category,
    TemtemPriorityView Priority,
    int? Damage,
    int? Stamina,
    // Tours de chargement avant de pouvoir l'utiliser ; nul si elle est disponible tout de suite.
    int? ChargeTurns,
    IReadOnlyList<string> Targets);
