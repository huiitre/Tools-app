using Tools.Api.Modules.Temtem.Techniques.Application.Views;
using Tools.Api.Modules.Temtem.Traits.Application.Views;
using Tools.Api.Modules.Temtem.Types.Application.Views;

namespace Tools.Api.Modules.Temtem.Creatures.Application.Views;

public sealed record TemtemStatsView(
    int Hp,
    int Stamina,
    int Speed,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense);

// **La vue de base, réutilisée partout** : carte du catalogue, vignette d'équipe, ligne du
// simulateur. Elle porte tout ce qui identifie un Temtem et ses types — c'est-à-dire tout ce dont
// le calcul d'efficacité a besoin.
public sealed record TemtemSummaryView(
    int Id,
    string Slug,
    string Name,
    string? ImageUrl,
    TemtemTypeView Type1,
    TemtemTypeView? Type2,
    TemtemStatsView Stats);

// Comment un Temtem apprend une technique : le niveau n'est renseigné que pour LEVEL.
public sealed record TemtemLearnedTechniqueView(
    TemtemTechniqueView Technique,
    string Source,
    int? Level);

// Le résumé **plus** ce qui ne sert qu'à la fiche et à la composition d'équipe. Le résumé y est
// imbriqué au lieu d'être recopié : une seule définition des champs communs.
public sealed record TemtemDetailView(
    TemtemSummaryView Temtem,
    IReadOnlyList<TemtemLearnedTechniqueView> Techniques,
    IReadOnlyList<TemtemTraitView> Traits);
