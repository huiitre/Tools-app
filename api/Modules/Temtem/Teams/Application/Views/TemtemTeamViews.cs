using Tools.Api.Modules.Temtem.Creatures.Application.Views;
using Tools.Api.Modules.Temtem.Techniques.Application.Views;

namespace Tools.Api.Modules.Temtem.Teams.Application.Views;

// Un membre : le Temtem, sa place, et les techniques retenues pour lui.
//
// `Temtem` est le résumé du catalogue, tel quel — la vignette d'équipe et la carte de la grille
// portent les mêmes champs, définis une seule fois. `Techniques` liste ce que le joueur a choisi,
// pas ce que le Temtem sait faire : la liste complète se lit sur sa fiche.
public sealed record TemtemTeamMemberView(
    long Id,
    int Slot,
    TemtemSummaryView Temtem,
    IReadOnlyList<TemtemTechniqueView> Techniques);

public sealed record TemtemTeamView(
    long Id,
    string Name,
    IReadOnlyList<TemtemTeamMemberView> Members,
    DateTime CreatedAt,
    DateTime UpdatedAt);
